using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
namespace QuanLySanBai.Controllers;
public class CourtsController(QuanLySanBaiContext db, BookingService service) : Controller
{
    public async Task<IActionResult> Index(int? sportId,string? area,DateOnly? date,long? selectedCourtId=null,int page=1)
    {
        if(!ModelState.IsValid || page<1 || page>10000) return BadRequest();
        var query=service.PublicCourts
            .Include(s=>s.CoSo)
            .Include(s=>s.LoaiSan).ThenInclude(t=>t.MonTheThao)
            .Include(s=>s.HinhAnhSans)
            .AsQueryable();
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

        var courtIds = courts.Select(c => c.Id).ToList();
        var refDate = date ?? VietnamTime.Today;
        var minPrices = courtIds.Count == 0
            ? new Dictionary<long, decimal>()
            : await db.BangGia.AsNoTracking()
                .Where(g => courtIds.Contains(g.SanId) && g.DangApDung && g.NgayHieuLucTu <= refDate && (g.NgayHieuLucDen == null || g.NgayHieuLucDen >= refDate))
                .GroupBy(g => g.SanId)
                .Select(g => new { SanId = g.Key, MinPrice = g.Min(x => x.GiaThue) })
                .ToDictionaryAsync(x => x.SanId, x => x.MinPrice);

        var previewTarget = (selectedCourtId.HasValue ? courts.FirstOrDefault(c => c.Id == selectedCourtId.Value) : null) ?? courts.FirstOrDefault();
        ViewBag.PreviewCalendar = previewTarget is not null ? await service.Calendar(previewTarget.Id, refDate) : null;
        ViewBag.MinPrices = minPrices;
        if (User.Identity?.IsAuthenticated == true)
        {
            var uid = AccessService.UserId(User);
            ViewBag.WalletBalance = await db.ViNguoiDungs.AsNoTracking()
                .Where(w => w.NguoiDungId == uid)
                .Select(w => (decimal?)w.SoDuKhaDung)
                .SingleOrDefaultAsync() ?? 0m;
        }
        ViewBag.Sports=await db.MonTheThaos.Where(m=>m.TrangThai).ToListAsync(); ViewBag.Date=date; ViewBag.Area=area; ViewBag.SportId=sportId; ViewBag.Page=page; ViewBag.SelectedCourtId=previewTarget?.Id;
        return View(courts);
    }
    public async Task<IActionResult> Details(long id,DateOnly? date)
    {
        if(!ModelState.IsValid) return BadRequest();
        var calendar=await service.Calendar(id,date ?? VietnamTime.Today);
        if (calendar is null) return NotFound();
        if (User.Identity?.IsAuthenticated == true)
        {
            var uid = AccessService.UserId(User);
            ViewBag.WalletBalance = await db.ViNguoiDungs.AsNoTracking()
                .Where(w => w.NguoiDungId == uid)
                .Select(w => (decimal?)w.SoDuKhaDung)
                .SingleOrDefaultAsync() ?? 0m;
            var targetDate = calendar.Date;
            var mySlots = await db.ChiTietDatSans.AsNoTracking()
                .Where(c => c.SanId == id
                    && c.NgaySuDung == targetDate
                    && c.DonDatSan.NguoiDungId == uid
                    && c.TrangThaiSlot != "DA_HUY"
                    && c.TrangThaiSlot != "DA_DOI"
                    && c.DonDatSan.TrangThai != "DA_HUY"
                    && c.DonDatSan.TrangThai != "HET_HAN")
                .Select(c => new { c.KhungGioId, c.DonDatSanId, c.DonDatSan.TrangThai })
                .ToListAsync();
            ViewBag.MySlotOrders = mySlots
                .GroupBy(x => x.KhungGioId)
                .ToDictionary(g => g.Key, g => (g.First().DonDatSanId, g.First().TrangThai));
        }
        return View(calendar);
    }
}
