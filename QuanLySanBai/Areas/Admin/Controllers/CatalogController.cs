using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;
namespace QuanLySanBai.Areas.Admin.Controllers;
[Area("Admin"),Authorize(Roles="ADMIN")]
public class CatalogController(QuanLySanBaiContext db) : Controller
{
    public async Task<IActionResult> Index() { ViewBag.Types=await db.LoaiSans.Include(t=>t.MonTheThao).ToListAsync(); return View(await db.MonTheThaos.ToListAsync()); }
    [HttpPost] public async Task<IActionResult> Sport(SportForm form)
    {
        if(!ModelState.IsValid) throw new BusinessException("Mã/tên môn không hợp lệ.");
        var sport=form.Id==0 ? new MonTheThao() : await db.MonTheThaos.FindAsync(form.Id) ?? throw new BusinessException("Không tìm thấy môn.");
        if(form.Id==0) db.MonTheThaos.Add(sport);
        sport.MaMon=form.Code.Trim(); sport.TenMon=form.Name.Trim(); sport.TrangThai=form.Active;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> CourtType(CourtTypeForm form)
    {
        if(!ModelState.IsValid || !await db.MonTheThaos.AnyAsync(s=>s.Id==form.SportId)) throw new BusinessException("Loại sân/môn/sức chứa không hợp lệ.");
        var type=form.Id==0 ? new LoaiSan() : await db.LoaiSans.FindAsync(form.Id) ?? throw new BusinessException("Không tìm thấy loại sân.");
        if(form.Id!=0 && type.MonTheThaoId!=form.SportId && await db.Sans.AnyAsync(s=>s.LoaiSanId==form.Id)) throw new BusinessException("Không đổi môn của loại sân đang được sử dụng.");
        if(form.Id==0) db.LoaiSans.Add(type);
        type.MonTheThaoId=form.SportId; type.TenLoai=form.Name.Trim(); type.SucChuaNguoi=form.Capacity;
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
}
