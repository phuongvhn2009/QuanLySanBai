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
    public async Task<IActionResult> Facilities() => View(await db.CoSos.AsNoTracking().Include(c => c.ChuSoHuu).OrderBy(c => c.Id).Take(200).ToListAsync());
    [HttpPost] public async Task<IActionResult> Review(long id, string decision)
    {
        if (decision is not ("DA_DUYET" or "TU_CHOI")) return BadRequest();
        var facility = await db.CoSos.FindAsync(id);
        if (facility is null) return NotFound();
        facility.TrangThaiDuyet = decision; facility.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Facilities));
    }
}
