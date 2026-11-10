using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
namespace QuanLySanBai.Controllers;
public class CourtsController(QuanLySanBaiContext db, BookingService service) : Controller
{
    public async Task<IActionResult> Index(int? sportId,string? area,DateOnly? date,int page=1)
    {
        if(!ModelState.IsValid || page<1 || page>10000) return BadRequest();
        var query=service.PublicCourts.Include(s=>s.CoSo).Include(s=>s.LoaiSan).ThenInclude(t=>t.MonTheThao).AsQueryable();
        if(sportId.HasValue) query=query.Where(s=>s.LoaiSan.MonTheThaoId==sportId);
        if(!string.IsNullOrWhiteSpace(area)) query=query.Where(s=>s.CoSo.DiaChi.Contains(area));
        List<Models.Entities.San> courts;
        if(date.HasValue)
        {
            // Filter availability before paging, so unavailable courts cannot hide later matches.
            var candidates=await query.OrderBy(s=>s.Id).ToListAsync();
            var filtered=new List<Models.Entities.San>();
            foreach(var s in candidates) if((await service.Calendar(s.Id,date.Value))?.Slots.Any(k=>k.CanBook)==true) filtered.Add(s);
            courts=filtered.Skip((page-1)*24).Take(24).ToList();
        }
        else courts=await query.OrderBy(s=>s.Id).Skip((page-1)*24).Take(24).ToListAsync();
        ViewBag.Sports=await db.MonTheThaos.Where(m=>m.TrangThai).ToListAsync(); ViewBag.Date=date; ViewBag.Area=area; ViewBag.SportId=sportId; ViewBag.Page=page;
        return View(courts);
    }
    public async Task<IActionResult> Details(long id,DateOnly? date)
    {
        if(!ModelState.IsValid) return BadRequest();
        var calendar=await service.Calendar(id,date ?? VietnamTime.Today);
        return calendar is null ? NotFound() : View(calendar);
    }
}
