using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Controllers;
[Authorize]
public class BookingsController(QuanLySanBaiContext db, BookingService service) : Controller
{
    [HttpPost] public async Task<IActionResult> Cancel([FromServices] OperationService operations,OrderActionForm f)
    {
        if(!ModelState.IsValid) return BadRequest();
        if(!await db.DonDatSans.AnyAsync(d=>d.Id==f.Id && d.NguoiDungId==AccessService.UserId(User)))return NotFound();
        await operations.Cancel(AccessService.UserId(User),f);return RedirectToAction(nameof(Details),new { id=f.Id });
    }
    [HttpPost] public async Task<IActionResult> Create(BookingForm form)
    {
        if(!ModelState.IsValid) throw new BusinessException("Dữ liệu đặt sân không hợp lệ.");
        var id=await service.Book(AccessService.UserId(User),form,HttpContext.RequestAborted);
        return RedirectToAction(nameof(Details),new { id });
    }
    public async Task<IActionResult> Index() => View(await db.DonDatSans.AsNoTracking().Where(d=>d.NguoiDungId==AccessService.UserId(User)).OrderByDescending(d=>d.Id).Take(100).ToListAsync());
    public async Task<IActionResult> Details(long id)
    {
        var order=await db.DonDatSans.AsNoTracking().Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.San).Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.ChinhSachHuyDoi).ThenInclude(p=>p.MucPhiHuyDois).SingleOrDefaultAsync(d=>d.Id==id && d.NguoiDungId==AccessService.UserId(User));
        if(order is null) return NotFound();
        ViewBag.CanCancel=!await db.ThanhToanDons.AnyAsync(p=>p.DonDatSanId==id && (p.TrangThai=="THANH_CONG" || p.TrangThai=="DANG_XU_LY"));
        ViewBag.ServerNow=await service.ServerNow(); return View(order);
    }
}
