using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Areas.CoSo.Controllers;
[Area("CoSo"),Authorize]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class OrdersController(QuanLySanBaiContext db,OperationService service) : Controller
{
    private long Actor=>AccessService.UserId(User);
    public async Task<IActionResult> Index(long facilityId,DateOnly? date,string? status,string? phone,int page=1)
    {
        if(!ModelState.IsValid) return BadRequest();await service.Require(Actor,facilityId,"XEM_LICH");
        var q=db.DonDatSans.AsNoTracking().Where(d=>d.CoSoId==facilityId);
        if(date.HasValue) q=q.Where(d=>d.ChiTietDatSans.Any(c=>c.NgaySuDung==date));
        if(!string.IsNullOrEmpty(status))q=q.Where(d=>d.TrangThai==status);
        if(!string.IsNullOrWhiteSpace(phone))q=q.Where(d=>d.SdtKhachHang.Contains(phone));
        var pages=Math.Max(1,(int)Math.Ceiling(await q.CountAsync()/20d));page=Math.Clamp(page,1,pages);
        ViewBag.CanCreate=await db.CoSos.AnyAsync(c=>c.Id==facilityId && c.ChuSoHuuId==Actor) || await new AccessService(db).HasPermission(Actor,facilityId,"TAO_DON");
        return View(new OperationList(await q.Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.San).OrderByDescending(d=>d.Id).Skip((page-1)*20).Take(20).ToListAsync(),facilityId,page,pages,date,status,phone));
    }
    public async Task<IActionResult> Details(long id)=>View(await service.Details(Actor,id));
    [HttpGet] public async Task<IActionResult> Counter(long facilityId)
    {
        await service.Require(Actor,facilityId,"TAO_DON");ViewBag.Courts=await db.Sans.Where(s=>s.CoSoId==facilityId && s.DangHoatDong).ToListAsync();ViewBag.Slots=await db.KhungGioMacDinhs.Where(k=>k.CoSoId==facilityId).ToListAsync();return View(new CounterForm { FacilityId=facilityId });
    }
    [HttpPost] public async Task<IActionResult> Counter(CounterForm f) { if(!ModelState.IsValid) throw new BusinessException("Kiểm tra sân, ngày, tên và số điện thoại.");return RedirectToAction(nameof(Details),new { id=await service.Counter(Actor,f) }); }
    private IActionResult Done(long id) { TempData["SuccessMessage"]="Thao tác thành công.";return RedirectToAction(nameof(Details),new { id }); }
    [HttpPost] public async Task<IActionResult> Collect(CollectForm f) { if(!ModelState.IsValid) return BadRequest();await service.Collect(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> Confirm(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Confirm(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> CheckIn(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.CheckIn(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> Complete(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Complete(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> Cancel(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Cancel(Actor,f);return Done(f.Id); }
}
