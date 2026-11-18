using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class WalletController(WalletService walletService) : Controller
{
    private long Actor => AccessService.UserId(User);

    [HttpGet]
    public async Task<IActionResult> Index() => View(await walletService.GetWalletIndex(Actor));

    [HttpPost]
    public async Task<IActionResult> NapTien(DepositForm form)
    {
        if (!ModelState.IsValid)
            throw new BusinessException("Số tiền nạp ví không hợp lệ (tối thiểu 1.000 đ).");

        var gatewayTxId = await walletService.CreateDepositQr(Actor, form.Amount);
        return RedirectToAction("Qr", "ThanhToan", new { id = gatewayTxId });
    }

    [HttpPost]
    public async Task<IActionResult> RutTien(WithdrawForm form)
    {
        if (!ModelState.IsValid)
            throw new BusinessException("Vui lòng kiểm tra số tiền rút (tối thiểu 10.000 đ) và thông tin ngân hàng.");

        await walletService.RequestWithdrawal(Actor, form);
        TempData["SuccessMessage"] = "Đã tạo yêu cầu rút tiền và tạm giữ số dư chờ duyệt.";
        return RedirectToAction(nameof(Index));
    }
}
