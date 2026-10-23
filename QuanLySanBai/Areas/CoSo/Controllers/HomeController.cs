using Microsoft.AspNetCore.Mvc;

namespace QuanLySanBai.Areas.CoSo.Controllers;

[Area("CoSo")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}