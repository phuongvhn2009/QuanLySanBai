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
        var access = new AccessService(db);
        ViewBag.CanCreate = await db.CoSos.AnyAsync(c => c.Id == facilityId && c.ChuSoHuuId == Actor) || await access.HasPermission(Actor, facilityId, "TAO_DON");
        ViewBag.CanCheckIn = await access.HasPermission(Actor, facilityId, "CHECK_IN");
        return View(new OperationList(await q.Include(d=>d.ChiTietDatSans).ThenInclude(c=>c.San).OrderByDescending(d=>d.Id).Skip((page-1)*20).Take(20).ToListAsync(),facilityId,page,pages,date,status,phone));
    }
    public async Task<IActionResult> Details([FromServices] RefundService refunds, long id)
    {
        var model = await service.Details(Actor, id);
        var refundsList = await db.HoanTiens.AsNoTracking()
            .Where(h => h.ThanhToanDon.DonDatSanId == id)
            .OrderByDescending(h => h.Id)
            .ToListAsync();
        ViewBag.Refunds = refundsList;

        var activeSlot = model.Order.ChiTietDatSans.FirstOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"));
        if (model.Cancel && model.Paid > 0 && model.Order.TrangThai is ("CHO_THANH_TOAN" or "DA_XAC_NHAN") && activeSlot is not null && VietnamTime.Start(activeSlot.NgaySuDung, activeSlot.GioBatDau) > VietnamTime.Now)
        {
            try { ViewBag.CancelPreview = await refunds.PreviewCancel(Actor, id); } catch { }
        }
        return View(model);
    }
    [HttpGet] public async Task<IActionResult> Counter(long facilityId)
    {
        await service.Require(Actor,facilityId,"TAO_DON");ViewBag.Courts=await db.Sans.Where(s=>s.CoSoId==facilityId && s.DangHoatDong && !s.DangBaoTri).ToListAsync();ViewBag.Slots=await db.KhungGioMacDinhs.Where(k=>k.CoSoId==facilityId).ToListAsync();return View(new CounterForm { FacilityId=facilityId });
    }
    [HttpPost] public async Task<IActionResult> Counter(CounterForm f) { if(!ModelState.IsValid) throw new BusinessException("Kiểm tra sân, ngày, tên và số điện thoại.");return RedirectToAction(nameof(Details),new { id=await service.Counter(Actor,f) }); }
    private IActionResult Done(long id) { TempData["SuccessMessage"]="Thao tác thành công.";return RedirectToAction(nameof(Details),new { id }); }
    [HttpPost] public async Task<IActionResult> Collect(CollectForm f) { if(!ModelState.IsValid) return BadRequest();await service.Collect(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> Confirm(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Confirm(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> CheckIn(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.CheckIn(Actor,f);return Done(f.Id); }
    [HttpPost]
    public async Task<IActionResult> ScanCheckIn(long facilityId, string? code)
    {
        try
        {
            var orderId = await service.CheckInByCode(Actor, facilityId, code);
            TempData["SuccessMessage"] = "Check-in bằng QR thành công!";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { facilityId });
        }
    }
    [HttpPost] public async Task<IActionResult> Complete(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Complete(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> Cancel(OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await service.Cancel(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> CancelWithRefund([FromServices] RefundService refunds, OrderActionForm f) { if(!ModelState.IsValid)return BadRequest();await refunds.CancelWithRefund(Actor,f);return Done(f.Id); }
    [HttpPost] public async Task<IActionResult> CompleteManualRefund([FromServices] RefundService refunds, long orderId, long refundId, string? reference) { await refunds.CompleteManualRefund(Actor, refundId, reference); return Done(orderId); }
}
