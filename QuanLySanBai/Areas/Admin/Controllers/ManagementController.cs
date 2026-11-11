using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
namespace QuanLySanBai.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Roles = "ADMIN")]
public class ManagementController(QuanLySanBaiContext db) : Controller
{
    public async Task<IActionResult> Users(string? search)
    {
        var query = db.NguoiDungs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(u => u.HoVaTen.Contains(search) || u.SoDienThoai.Contains(search));
        return View(await query.OrderBy(u => u.Id).Take(200).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> SetActive(long id, bool active)
    {
        if (id == AccessService.UserId(User)) return BadRequest("Không thể tự khóa tài khoản quản trị đang sử dụng.");
        var user = await db.NguoiDungs.FindAsync(id);
        if (user is null) return NotFound();
        user.TrangThai = active; user.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Users));
    }
    public async Task<IActionResult> Facilities(string? status)
    {
        var query = db.CoSos.AsNoTracking().Include(c => c.ChuSoHuu).AsQueryable();
        if (status is "CHO_DUYET" or "DA_DUYET" or "TU_CHOI") query = query.Where(c => c.TrangThaiDuyet == status);
        ViewBag.Status = status;
        var list = await query.OrderBy(c => c.Id).Take(200).ToListAsync();
        var ids = list.Select(c => c.Id).ToList();
        var notes = await db.ThongBaos.AsNoTracking().Where(t => t.LoaiThamChieu == "CO_SO" && t.LoaiThongBao == "TU_CHOI_CO_SO" && t.MaThamChieuId != null && ids.Contains(t.MaThamChieuId.Value)).OrderByDescending(t => t.NgayTao).ToListAsync();
        ViewBag.RejectReasons = notes.GroupBy(t => t.MaThamChieuId!.Value).ToDictionary(g => g.Key, g => g.First().NoiDung);
        return View(list);
    }
    [HttpPost] public async Task<IActionResult> Review(long id, string decision, string? reason = null)
    {
        if (decision is not ("DA_DUYET" or "TU_CHOI")) return BadRequest();
        var facility = await db.CoSos.FindAsync(id);
        if (facility is null) return NotFound();
        facility.TrangThaiDuyet = decision; facility.NgayCapNhat = DateTime.Now;
        if (decision == "TU_CHOI" && !string.IsNullOrWhiteSpace(reason))
        {
            db.ThongBaos.Add(new Models.Entities.ThongBao { NguoiDungId = facility.ChuSoHuuId, TieuDe = $"Từ chối duyệt cơ sở: {facility.TenCoSo}", NoiDung = reason.Trim(), LoaiThongBao = "TU_CHOI_CO_SO", MaThamChieuId = facility.Id, LoaiThamChieu = "CO_SO", NgayTao = DateTime.Now });
        }
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = decision == "DA_DUYET" ? $"Đã duyệt cơ sở \"{facility.TenCoSo}\"." : $"Đã từ chối cơ sở \"{facility.TenCoSo}\".";
        return RedirectToAction(nameof(Facilities));
    }
}
