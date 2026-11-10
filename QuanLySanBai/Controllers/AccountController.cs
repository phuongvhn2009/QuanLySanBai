using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

public class AccountController(QuanLySanBaiContext db, IPasswordHasher<NguoiDung> hasher) : Controller
{
    [HttpGet] public IActionResult Register() => View(new RegisterModel());
    [HttpPost] public async Task<IActionResult> Register(RegisterModel model)
    {
        Normalize(model);
        await CheckDuplicates(model);
        if (!ModelState.IsValid) return View(model);
        var role = await db.VaiTros.SingleOrDefaultAsync(r => r.MaVaiTro == "KHACH_HANG");
        if (role is null) { ModelState.AddModelError("", "Chưa có danh mục vai trò khách hàng. Hãy liên hệ quản trị viên."); return View(model); }
        var user = new NguoiDung { HoVaTen = model.HoVaTen, SoDienThoai = model.SoDienThoai, Email = model.Email, TrangThai = true, NgayTao = DateTime.Now, NgayCapNhat = DateTime.Now };
        user.MatKhauHash = hasher.HashPassword(user, model.Password);
        user.NguoiDungVaiTros.Add(new NguoiDungVaiTro { VaiTroId = role.Id, NgayGan = DateTime.Now });
        db.NguoiDungs.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { ModelState.AddModelError("", "Số điện thoại hoặc email đã được sử dụng."); return View(model); }
        TempData["SuccessMessage"] = "Đăng ký thành công. Bạn có thể đăng nhập.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginModel { ReturnUrl = returnUrl });
    [HttpPost] public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var login = model.Login.Trim().ToLowerInvariant();
        var user = await db.NguoiDungs.Include(u => u.NguoiDungVaiTros).ThenInclude(r => r.VaiTro).SingleOrDefaultAsync(u => u.SoDienThoai == login || u.Email == login);
        var result = PasswordVerificationResult.Failed;
        if (user is not null && user.TrangThai)
        {
            try { result = hasher.VerifyHashedPassword(user, user.MatKhauHash, model.Password); }
            catch (FormatException) { }
        }
        if (user is null || result == PasswordVerificationResult.Failed) { ModelState.AddModelError("", "Thông tin đăng nhập không hợp lệ hoặc tài khoản đã ngừng hoạt động."); return View(model); }
        if (result == PasswordVerificationResult.SuccessRehashNeeded) { user.MatKhauHash = hasher.HashPassword(user, model.Password); await db.SaveChangesAsync(); }
        await HttpContext.SignInAsync(AccessService.Principal(user), new AuthenticationProperties { IsPersistent = model.RememberMe });
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
    }
    [Authorize, HttpPost] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    [Authorize, HttpGet] public async Task<IActionResult> Profile()
    {
        var user = await db.NguoiDungs.FindAsync(AccessService.UserId(User));
        if (user is null) return Challenge();
        return View(new ProfileModel { HoVaTen = user.HoVaTen, SoDienThoai = user.SoDienThoai, Email = user.Email });
    }
    [Authorize, HttpPost] public async Task<IActionResult> Profile(ProfileModel model)
    {
        var id = AccessService.UserId(User);
        Normalize(model); await CheckDuplicates(model, id);
        if (!ModelState.IsValid) return View(model);
        var user = await db.NguoiDungs.FindAsync(id);
        if (user is null) return Challenge();
        user.HoVaTen = model.HoVaTen; user.SoDienThoai = model.SoDienThoai; user.Email = model.Email; user.NgayCapNhat = DateTime.Now;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { ModelState.AddModelError("", "Số điện thoại hoặc email đã được sử dụng."); return View(model); }
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }
    [HttpGet] public IActionResult Denied() { Response.StatusCode = 403; return View(); }
    private static void Normalize(ProfileModel m) { m.HoVaTen = (m.HoVaTen ?? "").Trim(); m.SoDienThoai = (m.SoDienThoai ?? "").Trim(); m.Email = string.IsNullOrWhiteSpace(m.Email) ? null : m.Email.Trim().ToLowerInvariant(); }
    private async Task CheckDuplicates(ProfileModel m, long id = 0)
    {
        if (await db.NguoiDungs.AnyAsync(u => u.Id != id && u.SoDienThoai == m.SoDienThoai)) ModelState.AddModelError(nameof(m.SoDienThoai), "Số điện thoại đã được sử dụng.");
        if (m.Email != null && await db.NguoiDungs.AnyAsync(u => u.Id != id && u.Email == m.Email)) ModelState.AddModelError(nameof(m.Email), "Email đã được sử dụng.");
    }
    private static bool IsDuplicate(DbUpdateException e) => e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
}
