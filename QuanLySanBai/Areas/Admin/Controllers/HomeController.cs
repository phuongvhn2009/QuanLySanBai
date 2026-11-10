using Microsoft.AspNetCore.Mvc;

namespace QuanLySanBai.Areas.Admin.Controllers;

[Area("Admin"), Microsoft.AspNetCore.Authorization.Authorize(Roles = "ADMIN")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
