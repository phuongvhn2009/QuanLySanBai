using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    private IActionResult Fail(long id, string message) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Index), new { id }); }
    private IActionResult Invalid(long id) => Fail(id, "Dữ liệu biểu mẫu không hợp lệ. Hãy kiểm tra các trường.");
    private async Task<IActionResult> RunOnDashboard(long facilityId, Func<Task> action)
    {
        try
        {
            await action();
            return Done(facilityId);
        }
        catch (BusinessException ex)
        {
            return Fail(facilityId, ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            var msg = sql.Number switch
            {
                >= 51000 and < 51500 => sql.Message,
                2601 or 2627 => "Dữ liệu bị trùng. Kiểm tra tên, mã hoặc khoảng hiệu lực.",
                547 => "Dữ liệu không phù hợp ràng buộc hoặc đang được sử dụng.",
                1205 or -2 => "Dữ liệu đang được thao tác. Hãy tải lại và thử lại.",
                _ => "Không thể lưu dữ liệu. Vui lòng kiểm tra lại."
            };
            return Fail(facilityId, msg);
        }
    }
    public async Task<IActionResult> Index(long id) => View(await service.Dashboard(Actor,id));
    [HttpGet] public async Task<IActionResult> Facility(long id=0)
    {
        if (id==0) return View(new FacilityForm());
        var c=await service.Owned(Actor,id);
        ViewBag.ApprovalStatus=c.TrangThaiDuyet;
        return View(new FacilityForm { Id=c.Id, Name=c.TenCoSo, Address=c.DiaChi, Phone=c.SoDienThoaiHotline, Open=c.GioMoCua, Close=c.GioDongCua, Active=c.DangHoatDong });
    }
    [HttpPost] public async Task<IActionResult> Facility(FacilityForm model)
    {
        if(!ModelState.IsValid)
        {
            if (model.Id != 0) ViewBag.ApprovalStatus = (await service.Owned(Actor, model.Id)).TrangThaiDuyet;
            return View(model);
        }
        return Done(await service.SaveFacility(Actor,model));
    }
    [HttpGet] public async Task<IActionResult> Court(long facilityId, long id=0)
    {
        await service.Owned(Actor,facilityId);
        ViewBag.Types=await db.LoaiSans.Include(t=>t.MonTheThao).Where(t=>t.MonTheThao.TrangThai).ToListAsync();
        if(id==0) return View(new CourtForm { FacilityId=facilityId });
        var s=await db.Sans.SingleOrDefaultAsync(s=>s.Id==id && s.CoSoId==facilityId) ?? throw new FacilityAccessException();
        return View(new CourtForm { Id=id, FacilityId=facilityId, Name=s.TenSan, TypeId=s.LoaiSanId, Active=s.DangHoatDong, Maintenance=s.DangBaoTri });
    }
    [HttpPost] public async Task<IActionResult> Court(CourtForm model) { if(!ModelState.IsValid) return Invalid(model.FacilityId); return await RunOnDashboard(model.FacilityId, () => service.SaveCourt(Actor,model)); }
    [HttpPost] public async Task<IActionResult> Hours(HoursForm model) { if(!ModelState.IsValid) return Invalid(model.FacilityId); return await RunOnDashboard(model.FacilityId, () => service.SaveHours(Actor,model)); }
    [HttpPost] public async Task<IActionResult> Slot(SlotForm model) { if(!ModelState.IsValid) return Invalid(model.FacilityId); return await RunOnDashboard(model.FacilityId, () => service.AddSlot(Actor,model)); }
    [HttpPost] public async Task<IActionResult> DeleteSlot(long facilityId, int id) => await RunOnDashboard(facilityId, () => service.DeleteSlot(Actor, facilityId, id));
    [HttpPost] public async Task<IActionResult> Price(PriceForm model) { if(!ModelState.IsValid) return Invalid(model.FacilityId); return await RunOnDashboard(model.FacilityId, () => service.AddPrice(Actor,model)); }
    [HttpPost] public async Task<IActionResult> StopPrice(long facilityId,long id) => await RunOnDashboard(facilityId, () => service.StopPrice(Actor,facilityId,id));
    [HttpGet] public async Task<IActionResult> Policy(long facilityId) { await service.Owned(Actor,facilityId); return View(new PolicyForm { FacilityId=facilityId }); }
    [HttpPost] public async Task<IActionResult> Policy(PolicyForm model) { if(!ModelState.IsValid) return View(model); await service.AddPolicy(Actor,model); return Done(model.FacilityId); }
    [HttpPost] public async Task<IActionResult> ClosePolicy(long facilityId,long id,DateOnly until) { if(!ModelState.IsValid) return Invalid(facilityId); return await RunOnDashboard(facilityId, () => service.ClosePolicy(Actor,facilityId,id,until)); }
    [HttpPost] public async Task<IActionResult> Lock(LockForm model) { if(!ModelState.IsValid) return Invalid(model.FacilityId); return await RunOnDashboard(model.FacilityId, () => service.AddLock(Actor,model)); }
    [HttpPost] public async Task<IActionResult> CancelLock(long facilityId,long id) => await RunOnDashboard(facilityId, () => service.CancelLock(Actor,facilityId,id));
}
