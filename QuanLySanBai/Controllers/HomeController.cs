using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromServices] QuanLySanBai.Data.QuanLySanBaiContext db)
    {
        if(User.IsInRole("ADMIN")) return RedirectToAction("Index","Home",new { area="Admin" });
        var id=QuanLySanBai.Services.AccessService.UserId(User);
        if(id>0 && await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.CoSos,c=>c.ChuSoHuuId==id || c.ThanhVienCoSos.Any(m=>m.NguoiDungId==id && m.TrangThaiHoatDong))) return RedirectToAction("Index","Home",new { area="CoSo" });
        return RedirectToAction("Index","Courts");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
