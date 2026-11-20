using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Services;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Controllers;

[Authorize]
public class BookingsController(QuanLySanBaiContext db, BookingService service) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Cancel([FromServices] OperationService operations, OrderActionForm f)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!await db.DonDatSans.AnyAsync(d => d.Id == f.Id && d.NguoiDungId == AccessService.UserId(User))) return NotFound();
        await operations.Cancel(AccessService.UserId(User), f);
        return RedirectToAction(nameof(Details), new { id = f.Id });
    }

    [HttpPost]
    public async Task<IActionResult> HuyCoHoanTien([FromServices] RefundService refunds, OrderActionForm f)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (!await db.DonDatSans.AnyAsync(d => d.Id == f.Id && d.NguoiDungId == AccessService.UserId(User))) return NotFound();
        await refunds.CancelWithRefund(AccessService.UserId(User), f);
        TempData["SuccessMessage"] = "Đã hủy đơn và xử lý hoàn tiền theo chính sách.";
        return RedirectToAction(nameof(Details), new { id = f.Id });
    }

    [HttpGet]
    public async Task<IActionResult> DoiLich([FromServices] RescheduleService reschedules, long id, DateOnly? date)
    {
        var model = await reschedules.GetRescheduleOptions(AccessService.UserId(User), id, date);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DoiLich([FromServices] RescheduleService reschedules, RescheduleForm f)
    {
        if (!ModelState.IsValid || f.OrderId <= 0 || f.NewCourtId <= 0 || f.NewSlotId <= 0)
            throw new BusinessException("Thông tin đổi lịch không hợp lệ.");

        await reschedules.ExecuteReschedule(AccessService.UserId(User), f);
        TempData["SuccessMessage"] = "Đổi lịch đặt sân thành công!";
        return RedirectToAction(nameof(Details), new { id = f.OrderId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingForm form, [FromServices] PaymentService paymentService)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu đặt sân không hợp lệ. Vui lòng kiểm tra lại.";
            return form.CourtId > 0
                ? RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date != default ? form.Date.ToString("yyyy-MM-dd") : null })
                : RedirectToAction("Index", "Courts");
        }
        long id;
        var userId = AccessService.UserId(User);
        try
        {
            id = await service.Book(userId, form, HttpContext.RequestAborted);
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date.ToString("yyyy-MM-dd") });
        }
        catch (SqlException ex)
        {
            var msg = ex.Number switch
            {
                >= 51000 and < 51500 => ex.Message,
                2601 or 2627 => "Khung giờ này đã có người đặt trước. Vui lòng chọn khung giờ khác.",
                1205 or -2 => "Khung giờ đang có người thao tác. Hãy tải lại và thử lại.",
                _ => "Không thể hoàn tất đặt sân. Vui lòng thử lại sau."
            };
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Details", "Courts", new { id = form.CourtId, date = form.Date.ToString("yyyy-MM-dd") });
        }

        try
        {
            if (form.PaymentChoice is "QR_COC" or "QR_FULL")
            {
                var gwId = await paymentService.CreateBookingQr(userId, id, form.PaymentChoice == "QR_COC" ? "COC" : "THANH_TOAN");
                TempData["SuccessMessage"] = "Đã giữ chỗ thành công! Quét mã VietQR bên dưới để hoàn tất thanh toán.";
                return RedirectToAction("Qr", "ThanhToan", new { id = gwId });
            }

            if (form.PaymentChoice is "VI_COC" or "VI_FULL")
            {
                var payResult = await paymentService.PayWithWalletOrQrAsync(userId, new PayWalletForm
                {
                    OrderId = id,
                    Purpose = form.PaymentChoice == "VI_COC" ? "COC" : "THANH_TOAN",
                    Key = Guid.NewGuid().ToString("N")
                });
                if (payResult.PaymentId.HasValue)
                {
                    TempData["SuccessMessage"] = "Đặt sân và thanh toán bằng Ví thành công!";
                    return RedirectToAction("HoaDon", "ThanhToan", new { id = payResult.PaymentId.Value });
                }
                TempData["InfoMessage"] = payResult.InfoMessage;
                return RedirectToAction("Qr", "ThanhToan", new { id = payResult.GatewayTxId!.Value });
            }
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = $"Đã giữ chỗ đơn thành công nhưng chưa tạo được thanh toán: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = form.PaymentChoice == "TAI_QUAY"
            ? "Đặt giữ chỗ thành công! Vui lòng thanh toán tiền mặt hoặc chuyển khoản tại quầy trước hạn giữ chỗ."
            : "Đặt sân thành công! Bạn có thời gian giữ chỗ để hoàn tất các bước tiếp theo.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Index() => View(await db.DonDatSans.AsNoTracking()
        .Include(d => d.CoSo)
        .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San).ThenInclude(s => s.HinhAnhSans)
        .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San).ThenInclude(s => s.LoaiSan).ThenInclude(t => t.MonTheThao)
        .Where(d => d.NguoiDungId == AccessService.UserId(User))
        .OrderByDescending(d => d.Id)
        .Take(100)
        .ToListAsync());

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Details([FromServices] RefundService refunds, long id)
    {
        var userId = AccessService.UserId(User);
        var order = await db.DonDatSans.AsNoTracking()
            .Include(d => d.CoSo)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San).ThenInclude(s => s.LoaiSan).ThenInclude(t => t.MonTheThao)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.San).ThenInclude(s => s.HinhAnhSans)
            .Include(d => d.ChiTietDatSans).ThenInclude(c => c.ChinhSachHuyDoi).ThenInclude(p => p.MucPhiHuyDois)
            .Include(d => d.ThanhToanDons).ThenInclude(p => p.HoanTiens)
            .SingleOrDefaultAsync(d => d.Id == id && d.NguoiDungId == userId);
        if (order is null) return NotFound();

        var paid = OrderFinance.ComputeNetPaid(order.ThanhToanDons);
        var now = await service.ServerNow();
        var activeSlot = order.ChiTietDatSans.FirstOrDefault(c => c.TrangThaiSlot is not ("DA_DOI" or "DA_HUY"));

        ViewBag.Paid = paid;
        ViewBag.CanCancel = !order.ThanhToanDons.Any(p => p.TrangThai == "THANH_CONG" || p.TrangThai == "DANG_XU_LY");
        ViewBag.ServerNow = now;
        ViewBag.CanPay = order.TrangThai is ("CHO_THANH_TOAN" or "DA_XAC_NHAN" or "DANG_SU_DUNG") && paid < (order.TongTienPhaiTra ?? 0m) && (order.TrangThai != "CHO_THANH_TOAN" || order.ThoiGianHetHanGiuCho > now);
        if (order.TrangThai == "DA_XAC_NHAN" && !string.IsNullOrWhiteSpace(order.MaQrCheckin))
        {
            ViewBag.CheckInQrUri = QrCodeHelper.ToSvgDataUri(order.MaQrCheckin);
        }

        if (order.TrangThai is ("CHO_THANH_TOAN" or "DA_XAC_NHAN") && activeSlot is not null && VietnamTime.Start(activeSlot.NgaySuDung, activeSlot.GioBatDau) > now)
        {
            ViewBag.CancelPreview = await refunds.PreviewCancel(userId, id);
            ViewBag.CanReschedule = activeSlot.ChinhSachHuyDoi.ChoPhepDoiLich;
        }
        else
        {
            ViewBag.CanReschedule = false;
        }

        return View(order);
    }
}
