using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;

var builder = WebApplication.CreateBuilder(args.Where(a=>a!="--seed-demo").ToArray());

// Add services to the container.
builder.Services.AddControllersWithViews(o => { o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.Add<BusinessErrorFilter>(); });
builder.Services.AddDbContext<QuanLySanBaiContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<NguoiDung>, PasswordHasher<NguoiDung>>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<FacilityService>();
builder.Services.AddScoped<DevelopmentSeeder>();
builder.Services.AddScoped<BookingService>();
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

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
