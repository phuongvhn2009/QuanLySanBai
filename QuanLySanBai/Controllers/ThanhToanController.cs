using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ThanhToanController(
    PaymentService paymentService,
    WalletService walletService) : Controller
{
    private long Actor => AccessService.UserId(User);

    [HttpGet]
    public async Task<IActionResult> Index(long id)
    {
        var model = await paymentService.GetPaymentSelection(Actor, id);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TaoQr(CreateQrForm form)
    {
        if (!ModelState.IsValid || form.OrderId <= 0)
            throw new BusinessException("Yêu cầu tạo mã QR không hợp lệ.");

        var gatewayTxId = await paymentService.CreateBookingQr(Actor, form.OrderId, form.Purpose);
        return RedirectToAction(nameof(Qr), new { id = gatewayTxId });
    }

    [HttpGet]
    public async Task<IActionResult> Qr(long id)
    {
        var model = await paymentService.GetQrDetails(Actor, id);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TrangThai(long id)
    {
        var model = await paymentService.GetQrDetails(Actor, id);
        return Json(new
        {
            status = model.Status,
            orderStatus = model.OrderStatus,
            paymentId = model.PaymentId,
            isExpired = model.IsExpired
        });
    }

    [HttpPost]
    public async Task<IActionResult> ThanhToanVi(PayWalletForm form)
    {
        if (!ModelState.IsValid || form.OrderId <= 0)
            throw new BusinessException("Thông tin thanh toán ví không hợp lệ.");

        var paymentId = await walletService.PayBookingWithWallet(Actor, form);
        TempData["SuccessMessage"] = "Thanh toán bằng ví thành công.";
        return RedirectToAction(nameof(HoaDon), new { id = paymentId });
    }

    [HttpPost]
    public async Task<IActionResult> TienMat(ThanhToanTienMatVM form)
    {
        if (!ModelState.IsValid || form.OrderId <= 0)
            throw new BusinessException("Vui lòng nhập đầy đủ số tiền thu và tiền khách đưa hợp lệ.");

        var paymentId = await paymentService.CollectCash(Actor, form);
        TempData["SuccessMessage"] = "Thu tiền mặt thành công.";
        return RedirectToAction(nameof(HoaDon), new { id = paymentId });
    }

    [HttpGet]
    public async Task<IActionResult> HoaDon(long id)
    {
        var model = await paymentService.GetReceipt(Actor, id);
        return View(model);
    }
}
