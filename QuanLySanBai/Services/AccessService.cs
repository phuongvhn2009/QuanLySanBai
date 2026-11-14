using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.Services;

public class AccessService(QuanLySanBaiContext db)
{
    public static long UserId(ClaimsPrincipal user) => long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public Task<bool> CanManage(long userId, long facilityId) =>
        HasPermission(userId, facilityId, "QUAN_LY_THANH_VIEN");

    public async Task<bool> HasPermission(long userId, long facilityId, string permission) =>
        await db.CoSos.AnyAsync(c => c.Id == facilityId && c.TrangThaiDuyet == "DA_DUYET" &&
            (c.ChuSoHuuId == userId || c.ThanhVienCoSos.Any(m => m.NguoiDungId == userId && m.TrangThaiHoatDong &&
                m.PhanQuyenThanhViens.Any(p => p.QuyenHan.MaQuyen == permission))));

    public static ClaimsPrincipal Principal(NguoiDung user) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.HoVaTen) }
        .Concat(user.NguoiDungVaiTros.Select(r => new Claim(ClaimTypes.Role, r.VaiTro.MaVaiTro))), CookieAuthenticationDefaults.AuthenticationScheme));
}

public class LiveCookieEvents(QuanLySanBaiContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = AccessService.UserId(context.Principal!);
        var user = await db.NguoiDungs.Include(u => u.NguoiDungVaiTros).ThenInclude(r => r.VaiTro).SingleOrDefaultAsync(u => u.Id == id);
        if (user is null || !user.TrangThai)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        context.ReplacePrincipal(AccessService.Principal(user));
    }
}
