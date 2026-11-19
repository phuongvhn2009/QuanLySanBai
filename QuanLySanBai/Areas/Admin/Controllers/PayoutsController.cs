using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "ADMIN")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PayoutsController(QuanLySanBaiContext db, WalletService walletService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var withdrawals = await db.YeuCauRutTiens.AsNoTracking()
            .Include(w => w.Vi).ThenInclude(v => v.NguoiDung)
            .OrderByDescending(w => w.Id)
            .Take(100)
            .ToListAsync();

        var logs = await db.NhatKyWebhooks.AsNoTracking()
            .OrderByDescending(l => l.Id)
            .Take(100)
            .ToListAsync();

        return View(new AdminPayoutListVM
        {
            Withdrawals = withdrawals,
            WebhookLogs = logs
        });
    }

    [HttpPost]
    public async Task<IActionResult> Process(ProcessWithdrawalForm form)
    {
        await walletService.ProcessWithdrawal(AccessService.UserId(User), form);
        TempData["SuccessMessage"] = form.Approve ? "Đã duyệt chuyển khoản rút tiền." : "Đã từ chối yêu cầu rút tiền và giải tỏa số dư.";
        return RedirectToAction(nameof(Index));
    }
}
