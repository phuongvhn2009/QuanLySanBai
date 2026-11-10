using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Areas.CoSo.Controllers;

[Area("CoSo"), Authorize]
public class ConfigurationController(QuanLySanBaiContext db, FacilityService service) : Controller
{
    private long Actor => AccessService.UserId(User);
    private IActionResult Done(long id) { TempData["SuccessMessage"] = "Đã lưu cấu hình."; return RedirectToAction(nameof(Index), new { id }); }
    private IActionResult Invalid() => throw new BusinessException("Dữ liệu biểu mẫu không hợp lệ. Hãy quay lại và kiểm tra các trường.");
    public async Task<IActionResult> Index(long id) => View(await service.Dashboard(Actor,id));
    [HttpGet] public async Task<IActionResult> Facility(long id=0)
    {
        if (id==0) return View(new FacilityForm());
        var c=await service.Owned(Actor,id);
        return View(new FacilityForm { Id=c.Id, Name=c.TenCoSo, Address=c.DiaChi, Phone=c.SoDienThoaiHotline, Open=c.GioMoCua, Close=c.GioDongCua, Active=c.DangHoatDong });
    }
    [HttpPost] public async Task<IActionResult> Facility(FacilityForm model) { if(!ModelState.IsValid) return View(model); return Done(await service.SaveFacility(Actor,model)); }
    [HttpGet] public async Task<IActionResult> Court(long facilityId, long id=0)
    {
        await service.Owned(Actor,facilityId);
        ViewBag.Types=await db.LoaiSans.Include(t=>t.MonTheThao).Where(t=>t.MonTheThao.TrangThai).ToListAsync();
        if(id==0) return View(new CourtForm { FacilityId=facilityId });
        var s=await db.Sans.SingleOrDefaultAsync(s=>s.Id==id && s.CoSoId==facilityId) ?? throw new FacilityAccessException();
        return View(new CourtForm { Id=id, FacilityId=facilityId, Name=s.TenSan, TypeId=s.LoaiSanId, Active=s.DangHoatDong, Maintenance=s.DangBaoTri });
    }
    [HttpPost] public async Task<IActionResult> Court(CourtForm model) { if(!ModelState.IsValid) return Invalid(); await service.SaveCourt(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> Hours(HoursForm model) { if(!ModelState.IsValid) return Invalid(); await service.SaveHours(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> Slot(SlotForm model) { if(!ModelState.IsValid) return Invalid(); await service.AddSlot(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> Price(PriceForm model) { if(!ModelState.IsValid) return Invalid(); await service.AddPrice(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> StopPrice(long facilityId,long id) { await service.StopPrice(Actor,facilityId,id); return Done(facilityId); }
    [HttpGet] public async Task<IActionResult> Policy(long facilityId) { await service.Owned(Actor,facilityId); return View(new PolicyForm { FacilityId=facilityId }); }
    [HttpPost] public async Task<IActionResult> Policy(PolicyForm model) { if(!ModelState.IsValid) return View(model); await service.AddPolicy(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> ClosePolicy(long facilityId,long id,DateOnly until) { if(!ModelState.IsValid) return Invalid(); await service.ClosePolicy(Actor,facilityId,id,until); return Done(facilityId); }
    [HttpPost] public async Task<IActionResult> Lock(LockForm model) { if(!ModelState.IsValid) return Invalid(); await service.AddLock(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> CancelLock(long facilityId,long id) { await service.CancelLock(Actor,facilityId,id); return Done(facilityId); }
}
