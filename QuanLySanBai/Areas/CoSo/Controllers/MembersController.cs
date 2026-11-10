using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
namespace QuanLySanBai.Areas.CoSo.Controllers;
[Area("CoSo"), Authorize]
public class MembersController(QuanLySanBaiContext db, AccessService access) : Controller
{
    public async Task<IActionResult> Index(long coSoId)
    {
        if (!await access.CanManage(AccessService.UserId(User), coSoId)) return Forbid();
        ViewBag.CoSoId = coSoId;
        ViewBag.OwnerId=await db.CoSos.Where(c=>c.Id==coSoId).Select(c=>c.ChuSoHuuId).SingleAsync();
        ViewBag.Quyen = await db.QuyenHans.OrderBy(q => q.MaQuyen).ToListAsync();
        return View(await db.ThanhVienCoSos.Where(m => m.CoSoId == coSoId).Include(m => m.NguoiDung).Include(m => m.PhanQuyenThanhViens).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Add(long coSoId, string phone)
    {
        if (!await access.CanManage(AccessService.UserId(User), coSoId)) return Forbid();
        var user = await db.NguoiDungs.SingleOrDefaultAsync(u => u.SoDienThoai == phone && u.TrangThai);
        if (user is null) { TempData["ErrorMessage"] = "Không tìm thấy tài khoản đang hoạt động."; return RedirectToAction(nameof(Index), new { coSoId }); }
        if (await db.ThanhVienCoSos.AnyAsync(m => m.CoSoId == coSoId && m.NguoiDungId == user.Id)) { TempData["ErrorMessage"] = "Tài khoản đã là thành viên."; return RedirectToAction(nameof(Index), new { coSoId }); }
        db.ThanhVienCoSos.Add(new ThanhVienCoSo { CoSoId = coSoId, NguoiDungId = user.Id, VaiTroNoiBo = "NHAN_VIEN", TrangThaiHoatDong = true, NgayThamGia = DateTime.Now });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { TempData["ErrorMessage"] = "Tài khoản đã là thành viên."; }
        return RedirectToAction(nameof(Index), new { coSoId });
    }
    [HttpPost] public async Task<IActionResult> Update(long coSoId, long memberId, string vaiTro, bool active, int[] permissions)
    {
        var actor = AccessService.UserId(User);
        if (!await access.CanManage(actor, coSoId)) return Forbid();
        var member = await db.ThanhVienCoSos.Include(m => m.PhanQuyenThanhViens).SingleOrDefaultAsync(m => m.Id == memberId && m.CoSoId == coSoId);
        if (member is null) return NotFound();
        var owner = await db.CoSos.Where(c => c.Id == coSoId).Select(c => c.ChuSoHuuId).SingleAsync();
        if (member.NguoiDungId == owner || member.VaiTroNoiBo == "CHU_SAN" || member.NguoiDungId == actor) return Forbid();
        if (vaiTro is not ("NHAN_VIEN" or "QUAN_LY")) return BadRequest();
        if (actor != owner && (vaiTro == "QUAN_LY" || member.VaiTroNoiBo == "QUAN_LY")) return Forbid();
        var ids = permissions.Distinct().ToArray();
        if (await db.QuyenHans.CountAsync(q => ids.Contains(q.Id)) != ids.Length) return BadRequest();
        if (actor != owner)
        {
            var codes = await db.QuyenHans.Where(q => ids.Contains(q.Id)).Select(q => q.MaQuyen).ToListAsync();
            foreach (var code in codes) if (code == "QUAN_LY_THANH_VIEN" || !await access.HasPermission(actor, coSoId, code)) return Forbid();
        }
        member.VaiTroNoiBo = vaiTro; member.TrangThaiHoatDong = active;
        db.PhanQuyenThanhViens.RemoveRange(member.PhanQuyenThanhViens.Where(p => !ids.Contains(p.QuyenHanId)));
        foreach (var id in ids.Where(id => !member.PhanQuyenThanhViens.Any(p => p.QuyenHanId == id))) member.PhanQuyenThanhViens.Add(new PhanQuyenThanhVien { QuyenHanId = id });
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật thành viên và quyền.";
        return RedirectToAction(nameof(Index), new { coSoId });
    }
}
