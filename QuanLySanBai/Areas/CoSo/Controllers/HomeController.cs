using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
namespace QuanLySanBai.Areas.CoSo.Controllers;
[Area("CoSo"), Authorize]
public class HomeController(QuanLySanBaiContext db, AccessService access) : Controller
{
    public async Task<IActionResult> Index(long? coSoId)
    {
        var id = AccessService.UserId(User);
        ViewBag.ManagedIds=await db.CoSos.Where(c=>c.ChuSoHuuId==id || c.ThanhVienCoSos.Any(m=>m.NguoiDungId==id && m.TrangThaiHoatDong && (m.VaiTroNoiBo=="QUAN_LY" || m.VaiTroNoiBo=="CHU_SAN" || m.PhanQuyenThanhViens.Any(p=>p.QuyenHan.MaQuyen=="QUAN_LY_THANH_VIEN")))).Select(c=>c.Id).ToListAsync();
        if (coSoId.HasValue)
        {
            if (!await access.HasPermission(id, coSoId.Value, "XEM_LICH")) return Forbid();
            return View("Index");
        }
        return View("Facilities", await db.CoSos.Where(c => c.ChuSoHuuId == id || c.ThanhVienCoSos.Any(m => m.NguoiDungId == id && m.TrangThaiHoatDong)).ToListAsync());
    }
}
