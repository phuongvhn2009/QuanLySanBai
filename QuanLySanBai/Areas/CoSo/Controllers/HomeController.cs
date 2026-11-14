using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
namespace QuanLySanBai.Areas.CoSo.Controllers;
[Area("CoSo"), Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class HomeController(QuanLySanBaiContext db, AccessService access) : Controller
{
    public async Task<IActionResult> Index(long? coSoId)
    {
        var id = AccessService.UserId(User);
        var facilities=await db.CoSos.Where(c=>c.ChuSoHuuId==id || c.ThanhVienCoSos.Any(m=>m.NguoiDungId==id && m.TrangThaiHoatDong)).ToListAsync();
        var managedIds=new List<long>();
        foreach(var facility in facilities)
            if(await access.CanManage(id,facility.Id)) managedIds.Add(facility.Id);
        ViewBag.ManagedIds=managedIds;
        if (coSoId.HasValue)
        {
            if (!await access.HasPermission(id, coSoId.Value, "XEM_LICH")) return Forbid();
            return View("Index");
        }
        return View("Facilities", facilities);
    }
}
