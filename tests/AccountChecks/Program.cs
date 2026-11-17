using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLySanBai.Controllers;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
using QuanLySanBai.Areas.CoSo.Controllers;
using QuanLySanBai.Areas.Admin.Controllers;

await using (var masterConn = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True"))
{
    await masterConn.OpenAsync();
    await using var checkCmd = new SqlCommand("SELECT DB_ID(N'QuanLySanBai')", masterConn);
    if (await checkCmd.ExecuteScalarAsync() is null or DBNull)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (!File.Exists(Path.Combine(root, "Data", "QuanLySanBai_SQLServer.sql"))) root = Directory.GetCurrentDirectory();
        var script = await File.ReadAllTextAsync(Path.Combine(root, "Data", "QuanLySanBai_SQLServer.sql"));
        foreach (var batch in Regex.Split(script, @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) { await using var cmd = new SqlCommand(batch, masterConn) { CommandTimeout = 120 }; await cmd.ExecuteNonQueryAsync(); }
        var updatePath = Path.Combine(root, "Data", "Updates", "001_OrderRowVersion.sql");
        if (File.Exists(updatePath))
        {
            var updateSql = "USE [QuanLySanBai];\n" + await File.ReadAllTextAsync(updatePath);
            await using var cmd = new SqlCommand(updateSql, masterConn); await cmd.ExecuteNonQueryAsync();
        }
    }
}
var options = new DbContextOptionsBuilder<QuanLySanBaiContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=QuanLySanBai;Trusted_Connection=True;TrustServerCertificate=True").Options;
await using var db = new QuanLySanBaiContext(options);
await using var tx = await db.Database.BeginTransactionAsync();
var services = new ServiceCollection().AddLogging().AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie().Services.BuildServiceProvider();
var hasher = new PasswordHasher<NguoiDung>();
var access = new AccessService(db);
var passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
T Setup<T>(T c, NguoiDung? user = null) where T : Controller {
    var http = new DefaultHttpContext { RequestServices = services };
    if (user != null) http.User = AccessService.Principal(user);
    c.ControllerContext = new ControllerContext { HttpContext = http }; c.Url = new LocalUrls();
    c.TempData = new TempDataDictionary(http, new MemoryTemp());
    return c;
}
async Task<NguoiDung> Register(string phone, string? email = null) {
    var c = Setup(new AccountController(db, hasher));
    var r = await c.Register(new RegisterModel { HoVaTen = "Kiểm thử", SoDienThoai = phone, Email = email ?? (phone + "@gmail.com"), Password = "TestPassword123!", ConfirmPassword = "TestPassword123!" });
    Check(r is RedirectToActionResult, "Register " + phone);
    return await db.NguoiDungs.Include(u => u.NguoiDungVaiTros).ThenInclude(r => r.VaiTro).SingleAsync(u => u.SoDienThoai == phone);
}
try {
    var owner = await Register("0999000001", "example@gmail.com"); var staff = await Register("0999000002", "nguyenvana@gmail.com"); var stranger = await Register("0999000003", "bao123@gmail.com");
    Check(owner.Email == "example@gmail.com" && staff.Email == "nguyenvana@gmail.com" && stranger.Email == "bao123@gmail.com", "Valid Gmail accounts saved to database");
    foreach (var badEmail in new[] { "examplegmail.com", "example@gmail", "example@yahoo.com", "example@gmail.vn", "@gmail.com", "example@.com", "" })
    {
        var invalidCtrl = Setup(new AccountController(db, hasher));
        var invalidRes = await invalidCtrl.Register(new RegisterModel { HoVaTen = "Email lỗi", SoDienThoai = "0999000099", Email = badEmail, Password = "TestPassword123!", ConfirmPassword = "TestPassword123!" });
        var emailErrors = invalidCtrl.ModelState[nameof(RegisterModel.Email)]?.Errors.Select(e => e.ErrorMessage).ToList() ?? [];
        Check(invalidRes is ViewResult && !invalidCtrl.ModelState.IsValid && emailErrors.Contains(ProfileModel.GmailErrorMessage), "Invalid Gmail rejected with message: " + (badEmail.Length == 0 ? "<empty>" : badEmail));
        Check(!await db.NguoiDungs.AnyAsync(u => u.SoDienThoai == "0999000099"), "Invalid Gmail user not saved: " + (badEmail.Length == 0 ? "<empty>" : badEmail));
    }
    Check(owner.MatKhauHash != "TestPassword123!" && hasher.VerifyHashedPassword(owner, owner.MatKhauHash, "TestPassword123!") != PasswordVerificationResult.Failed, "Password hashed and verifiable");
    Check(owner.NguoiDungVaiTros.Single().VaiTro.MaVaiTro == "KHACH_HANG", "Registration grants customer only");
    var duplicate = Setup(new AccountController(db, hasher));
    Check(await duplicate.Register(new RegisterModel { HoVaTen="Trùng", SoDienThoai=owner.SoDienThoai, Email=owner.Email, Password="Another123!" }) is ViewResult && duplicate.ModelState.ErrorCount == 2, "Duplicate phone and email rejected");
    var badLogin = Setup(new AccountController(db, hasher));
    Check(await badLogin.Login(new LoginModel { Login=owner.SoDienThoai, Password="wrong" }) is ViewResult && !badLogin.ModelState.IsValid, "Wrong password rejected");
    var goodLogin = Setup(new AccountController(db, hasher));
    goodLogin.Url = new LocalUrls();
    Check(await goodLogin.Login(new LoginModel { Login=owner.SoDienThoai, Password="TestPassword123!", ReturnUrl="https://evil.test" }) is RedirectToActionResult, "Valid login and external redirect rejected");
    Check(goodLogin.Response.Headers.SetCookie.ToString().Contains("httponly", StringComparison.OrdinalIgnoreCase), "HttpOnly auth cookie issued");
    var profile = Setup(new AccountController(db, hasher), staff);
    await profile.Profile(new ProfileModel { HoVaTen="Tên mới", SoDienThoai=staff.SoDienThoai, Email=staff.Email });
    Check(staff.HoVaTen == "Tên mới" && owner.HoVaTen == "Kiểm thử", "Profile updates current user only");
    var facility = new CoSo { ChuSoHuuId=owner.Id, TenCoSo="Test", DiaChi="Test", GioMoCua=new TimeOnly(6,0), GioDongCua=new TimeOnly(22,0), TrangThaiDuyet="DA_DUYET", DangHoatDong=true, NgayTao=DateTime.Now, NgayCapNhat=DateTime.Now };
    db.CoSos.Add(facility); await db.SaveChangesAsync();
    Check(await access.CanManage(owner.Id, facility.Id) && !await access.CanManage(stranger.Id, facility.Id), "Facility owner allowed and stranger denied");
    var members = Setup(new MembersController(db, access), owner);
    await members.Add(facility.Id, staff.SoDienThoai);
    var member = await db.ThanhVienCoSos.SingleAsync(m => m.CoSoId == facility.Id && m.NguoiDungId == staff.Id);
    var right = await db.QuyenHans.SingleAsync(q => q.MaQuyen == "QUAN_LY_THANH_VIEN");
    await members.Update(facility.Id, member.Id, "NHAN_VIEN", true, [right.Id]);
    Check(await access.CanManage(staff.Id, facility.Id), "Grant permission effective");
    var staffController = Setup(new MembersController(db, access), staff);
    Check(await staffController.Update(facility.Id, member.Id, "QUAN_LY", true, []) is ForbidResult, "Self elevation denied");
    Check(await members.Update(facility.Id + 12345, member.Id, "NHAN_VIEN", true, []) is ForbidResult, "Cross facility ID tampering denied");
    await members.Update(facility.Id, member.Id, "NHAN_VIEN", true, []);
    Check(!await access.CanManage(staff.Id, facility.Id), "Revocation effective on next operation");
    Check(await staffController.Index(facility.Id) is ForbidResult, "Revoked member cannot call management action");
    var admin = Setup(new ManagementController(db), owner);
    await admin.Review(facility.Id, "DA_DUYET"); Check(facility.TrangThaiDuyet == "DA_DUYET", "Admin approve");
    await admin.Review(facility.Id, "TU_CHOI"); Check(facility.TrangThaiDuyet == "TU_CHOI", "Admin reject");
    Check(await admin.Review(facility.Id, "INVALID") is BadRequestResult, "Invalid review rejected");
    await admin.SetActive(staff.Id, false);
    var locked = Setup(new AccountController(db, hasher));
    Check(await locked.Login(new LoginModel { Login=staff.SoDienThoai, Password="TestPassword123!" }) is ViewResult && !locked.ModelState.IsValid, "Locked account cannot login");
    var http = new DefaultHttpContext { RequestServices=services };
    var ticket = new AuthenticationTicket(AccessService.Principal(staff), CookieAuthenticationDefaults.AuthenticationScheme);
    var validation = new CookieValidatePrincipalContext(http, new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)), new CookieAuthenticationOptions(), ticket);
    await new LiveCookieEvents(db).ValidatePrincipal(validation);
    Check(validation.Principal is null, "Existing session rejected after account lock");
    Console.WriteLine($"{passed} checks passed; test data rolled back.");
} finally { await tx.RollbackAsync(); }
// Optional end-to-end checks against a running local application.
if (args.Length > 0)
{
    await using var httpDb = new QuanLySanBaiContext(options);
    var phone = "09" + Random.Shared.Next(10000000, 99999999);
    NguoiDung? fixture = null;
    CoSo? fixtureFacility = null;
    var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new System.Net.CookieContainer() };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(args[0]) };
    async Task<HttpResponseMessage> Post(string path, Dictionary<string,string> values, string? tokenPath = null)
    {
        var html = await client.GetStringAsync(tokenPath ?? path);
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Check(token.Length > 0, "CSRF token rendered " + path);
        values["__RequestVerificationToken"] = System.Net.WebUtility.HtmlDecode(token);
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }
    try
    {
        foreach (var badEmail in new[] { "examplegmail.com", "example@gmail", "example@yahoo.com", "example@gmail.vn", "@gmail.com", "example@.com", "" })
        {
            var badResp = await Post("/Account/Register", new() { ["HoVaTen"]="HTTP bad email", ["SoDienThoai"]=phone, ["Email"]=badEmail, ["Password"]="HttpTest123!", ["ConfirmPassword"]="HttpTest123!" });
            var badHtml = System.Net.WebUtility.HtmlDecode(await badResp.Content.ReadAsStringAsync());
            Check((int)badResp.StatusCode == 200 && badHtml.Contains(ProfileModel.GmailErrorMessage), "HTTP invalid Gmail rejected: " + (badEmail.Length == 0 ? "<empty>" : badEmail));
            Check(!await httpDb.NguoiDungs.AnyAsync(u => u.SoDienThoai == phone), "HTTP invalid Gmail not persisted: " + (badEmail.Length == 0 ? "<empty>" : badEmail));
        }
        var response = await Post("/Account/Register", new() { ["HoVaTen"]="HTTP test", ["SoDienThoai"]=phone, ["Email"]=$"httptest{phone}@gmail.com", ["Password"]="HttpTest123!", ["ConfirmPassword"]="HttpTest123!", ["Role"]="ADMIN" });
        Check((int)response.StatusCode == 302, "HTTP register");
        fixture = await httpDb.NguoiDungs.SingleAsync(u => u.SoDienThoai == phone);
        Check(!await httpDb.NguoiDungVaiTros.AnyAsync(r => r.NguoiDungId == fixture.Id && r.VaiTro.MaVaiTro == "ADMIN"), "Forged registration role ignored");
        response = await Post("/Account/Login", new() { ["Login"]=phone, ["Password"]="HttpTest123!" });
        Check((int)response.StatusCode == 302, "HTTP login");
        response = await client.GetAsync("/Admin/Management/Users");
        Check((int)response.StatusCode == 302 && response.Headers.Location!.ToString().Contains("Denied"), "HTTP ordinary user denied Admin");
        Check((await client.GetStringAsync("/Account/Profile")).Contains(phone), "HTTP profile rendered for current user");
        var adminRole = await httpDb.VaiTros.SingleAsync(r => r.MaVaiTro == "ADMIN");
        httpDb.NguoiDungVaiTros.Add(new NguoiDungVaiTro { NguoiDungId=fixture.Id, VaiTroId=adminRole.Id, NgayGan=DateTime.Now });
        fixtureFacility = new CoSo { ChuSoHuuId=fixture.Id, TenCoSo="HTTP fixture", DiaChi="Test", GioMoCua=new TimeOnly(6,0), GioDongCua=new TimeOnly(22,0), TrangThaiDuyet="DA_DUYET", DangHoatDong=true, NgayTao=DateTime.Now, NgayCapNhat=DateTime.Now };
        httpDb.CoSos.Add(fixtureFacility); await httpDb.SaveChangesAsync();
        Check((int)(await client.GetAsync("/Admin/Management/Users")).StatusCode == 200, "HTTP admin user list renders");
        Check((int)(await client.GetAsync("/Admin/Management/Facilities")).StatusCode == 200, "HTTP admin facility list renders");
        Check((int)(await client.GetAsync("/CoSo")).StatusCode == 200, "HTTP owned facilities render");
        var member = new ThanhVienCoSo { NguoiDungId=fixture.Id, CoSoId=fixtureFacility.Id, VaiTroNoiBo="CHU_SAN", TrangThaiHoatDong=true, NgayThamGia=DateTime.Now };
        httpDb.ThanhVienCoSos.Add(member); await httpDb.SaveChangesAsync();
        Check((int)(await client.GetAsync("/CoSo/Members?coSoId="+fixtureFacility.Id)).StatusCode == 200, "HTTP member management renders with member");
        response = await Post("/Account/Logout", new(), "/Account/Profile");
        Check((int)response.StatusCode == 302 && (int)(await client.GetAsync("/Account/Profile")).StatusCode == 302, "HTTP logout removes session");
        Console.WriteLine($"Total {passed} checks passed.");
    }
    finally
    {
        // Remove only the uniquely named fixture created by this run.
        fixture ??= await httpDb.NguoiDungs.SingleOrDefaultAsync(u => u.SoDienThoai == phone);
        if (fixture != null)
        {
            var members = await httpDb.ThanhVienCoSos.Where(m => m.NguoiDungId == fixture.Id).ToListAsync();
            httpDb.ThanhVienCoSos.RemoveRange(members);
            httpDb.CoSos.RemoveRange(await httpDb.CoSos.Where(c => c.ChuSoHuuId == fixture.Id).ToListAsync());
            httpDb.NguoiDungVaiTros.RemoveRange(await httpDb.NguoiDungVaiTros.Where(r => r.NguoiDungId == fixture.Id).ToListAsync());
            httpDb.NguoiDungs.Remove(fixture);
            await httpDb.SaveChangesAsync();
        }
    }
}class MemoryTemp : ITempDataProvider { public IDictionary<string,object> LoadTempData(HttpContext c) => new Dictionary<string,object>(); public void SaveTempData(HttpContext c, IDictionary<string,object> v) {} }
class LocalUrls : IUrlHelper {
    public ActionContext ActionContext => new(); public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext c)=>null; public string? Content(string? s)=>s;
    public bool IsLocalUrl(string? s)=>s?.StartsWith('/') == true && !s.StartsWith("//"); public string? Link(string? n, object? v)=>null; public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext c)=>null;
}
