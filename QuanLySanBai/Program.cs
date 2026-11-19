using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;

var builder = WebApplication.CreateBuilder(args.Where(a=>a!="--seed-demo").ToArray());
builder.Services.AddSignalR();
builder.Services.AddScoped<RealtimeUpdates>();

// Add services to the container.
builder.Services.AddControllersWithViews(o => { o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.Add<BusinessErrorFilter>(); });
builder.Services.AddDbContext<QuanLySanBaiContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<NguoiDung>, PasswordHasher<NguoiDung>>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<FacilityService>();
builder.Services.AddScoped<CourtImageService>();
builder.Services.AddScoped<DevelopmentSeeder>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<OperationService>();
builder.Services.AddScoped<VietQrService>();
builder.Services.AddScoped<SePayService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<RescheduleService>();
builder.Services.AddOptions<QuanLySanBai.ViewModels.SePayOptions>().BindConfiguration("SePay");
builder.Services.AddOptions<CourtImageOptions>().BindConfiguration("CourtImages").Validate(o => o.MaxFileSizeBytes is >= 102400 and <= 20_971_520 && o.MaxImagesPerCourt is >= 1 and <= 30, "Invalid CourtImages options").ValidateOnStart();
builder.Services.AddOptions<QuanLySanBai.ViewModels.OperationOptions>().BindConfiguration("Operations").Validate(o=>o.CheckInEarlyMinutes is >=0 and <=120).ValidateOnStart();
builder.Services.AddOptions<QuanLySanBai.ViewModels.BookingOptions>().BindConfiguration("Booking").Validate(o=>o.HoldMinutes is >=1 and <=60 && o.ExpiryIntervalSeconds is >=5 and <=300,"Invalid booking options").ValidateOnStart();
builder.Services.AddHostedService<HoldExpiryWorker>();
builder.Services.AddScoped<LiveCookieEvents>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.EventsType = typeof(LiveCookieEvents);
});
builder.Services.AddAuthorization();

var app = builder.Build();

if(args.Contains("--seed-demo"))
{
    if(!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo seed is allowed only in Development.");
    using var scope=app.Services.CreateScope();
    var report=await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().Seed();
    var dir=Path.Combine(app.Environment.ContentRootPath,"..",".local"); Directory.CreateDirectory(dir);
    var path=Path.Combine(dir,"demo-accounts.txt");
    if(!File.Exists(path)) await File.WriteAllTextAsync(path,report);
    Console.WriteLine("Demo ready. Credentials: "+Path.GetFullPath(path));
    return;
}

// Configure the HTTP request pipeline.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// shortcut: in-memory per-IP rate limit resets on restart; move to distributed limiter when running multiple instances
var rateBuckets = new ConcurrentDictionary<string, (long Window, int Count)>();
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    if (context.Request.Path.Value is { } p && p.Contains("//", StringComparison.Ordinal))
    {
        while (p.Contains("//", StringComparison.Ordinal)) p = p.Replace("//", "/", StringComparison.Ordinal);
        context.Request.Path = p;
    }
    var reqPath = context.Request.Path;
    int limit = HttpMethods.IsPost(context.Request.Method) && context.Connection.RemoteIpAddress is not null
        ? (reqPath.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) || reqPath.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) ? 30
        : reqPath.Equals("/api/webhook/sepay", StringComparison.OrdinalIgnoreCase) ? 120 : 0) : 0;
    if (limit > 0)
    {
        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        if (rateBuckets.Count > 4096) foreach (var kv in rateBuckets) if (kv.Value.Window < window) rateBuckets.TryRemove(kv.Key, out _);
        var (_, count) = rateBuckets.AddOrUpdate($"{(limit == 30 ? "auth" : "wh")}:{context.Connection.RemoteIpAddress}", _ => (window, 1), (_, prev) => prev.Window == window ? (window, prev.Count + 1) : (window, 1));
        if (count > limit) { context.Response.StatusCode = StatusCodes.Status429TooManyRequests; context.Response.Headers["Retry-After"] = "60"; await context.Response.WriteAsync("Too many requests."); return; }
    }
    await next();
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<UpdatesHub>("/updates", options => options.CloseOnAuthenticationExpiration = true);

var uploadsRoot = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        ctx.Context.Response.Headers.TryAdd("Cache-Control", "public,max-age=604800");
    }
});

app.MapStaticAssets();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
    
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
