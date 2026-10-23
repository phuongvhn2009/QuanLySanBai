using Microsoft.AspNetCore.Mvc;

namespace QuanLySanBai.Areas.Admin.Controllers;

[Area("Admin")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}