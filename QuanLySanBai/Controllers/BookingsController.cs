using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

[Authorize]
public class BookingsController(QuanLySanBaiContext db, BookingService service) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(BookingForm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu đặt sân không hợp lệ. Vui lòng kiểm tra lại.";
            return form.CourtId > 0
                ? RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date != default ? form.Date.ToString("yyyy-MM-dd") : null })
                : RedirectToAction("Index", "Courts");
        }
        try
        {
            var id = await service.Book(AccessService.UserId(User), form, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Đặt sân thành công! Bạn có thời gian giữ chỗ để hoàn tất các bước tiếp theo.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date.ToString("yyyy-MM-dd") });
        }
        catch (SqlException ex)
        {
            var msg = ex.Number switch
            {
                >= 51000 and < 51500 => ex.Message,
                2601 or 2627 => "Khung giờ này đã có người đặt trước. Vui lòng chọn khung giờ khác.",
                1205 or -2 => "Khung giờ đang có người thao tác. Hãy tải lại và thử lại.",
                _ => "Không thể hoàn tất đặt sân. Vui lòng thử lại sau."
            };
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date.ToString("yyyy-MM-dd") });
        }
    }

    public async Task<IActionResult> Index() => View(await db.DonDatSans.AsNoTracking().Where(d => d.NguoiDungId == AccessService.UserId(User)).OrderByDescending(d => d.Id).Take(100).ToListAsync());

    public async Task<IActionResult> Details(long id)
    {
        var order = await db.DonDatSans.AsNoTracking()
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .SingleOrDefaultAsync(d => d.Id == id && d.NguoiDungId == AccessService.UserId(User));
        if (order is null) return NotFound();
        ViewBag.ServerNow = await service.ServerNow();
        return View(order);
    }
}
