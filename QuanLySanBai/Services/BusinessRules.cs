using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySanBai.Data;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.Services;

public class BusinessException(string message) : Exception(message);
public class InsufficientWalletBalanceException(decimal available, decimal required)
    : BusinessException($"Số dư ví khả dụng ({available:N0} đ) không đủ để thanh toán {required:N0} đ.")
{
    public decimal Available { get; } = available;
    public decimal Required { get; } = required;
}
public class FacilityAccessException : Exception;

public static class VietnamTime
{
    public static DateTime Now => DateTime.UtcNow.AddHours(7);
    public static DateOnly Today => DateOnly.FromDateTime(Now);
    public static int Weekday(DateOnly date) => ((int)date.DayOfWeek + 6) % 7 + 1;
    public static DateTime Start(DateOnly date, TimeOnly time) => date.ToDateTime(time);
    public static DateTime End(DateOnly date, TimeOnly time) => time == TimeOnly.MinValue ? date.AddDays(1).ToDateTime(time) : date.ToDateTime(time);
    public static bool ValidHours(TimeOnly start, TimeOnly end) => start < end || end == TimeOnly.MinValue && start > TimeOnly.MinValue;
}

public static class OrderFinance
{
    public static async Task<decimal> NetPaidAsync(QuanLySanBaiContext db, long orderId)
    {
        var grossPaid = await db.ThanhToanDons
            .Where(p => p.DonDatSanId == orderId && p.TrangThai == "THANH_CONG")
            .SumAsync(p => (decimal?)p.SoTien) ?? 0m;
        var refunded = await db.HoanTiens
            .Where(h => h.ThanhToanDon.DonDatSanId == orderId &&
                        h.ThanhToanDon.TrangThai == "THANH_CONG" &&
                        h.TrangThai == "THANH_CONG")
            .SumAsync(h => (decimal?)h.SoTien) ?? 0m;
        return Math.Max(0m, grossPaid - refunded);
    }

    public static decimal ComputeNetPaid(IEnumerable<ThanhToanDon> payments) =>
        Math.Max(0m,
            payments.Where(p => p.TrangThai == "THANH_CONG").Sum(p => p.SoTien) -
            payments.Where(p => p.TrangThai == "THANH_CONG").SelectMany(p => p.HoanTiens).Where(h => h.TrangThai == "THANH_CONG").Sum(h => h.SoTien));

    public static MucPhiHuyDoi? MatchFeeTier(ChinhSachHuyDoi policy, decimal hours) =>
        policy.MucPhiHuyDois.OrderBy(m => m.SoGioTuTruocTran)
            .FirstOrDefault(m => hours >= m.SoGioTuTruocTran && (m.SoGioDenTruocTran is null || hours < m.SoGioDenTruocTran))
        ?? policy.MucPhiHuyDois.OrderBy(m => m.SoGioTuTruocTran).FirstOrDefault();
}

public static class QrCodeHelper
{
    public static string ToSvgDataUri(string text)
    {
        using var generator = new QRCoder.QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCoder.QRCodeGenerator.ECCLevel.M);
        var svg = new QRCoder.SvgQRCode(data).GetGraphic(6);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }
}

public class BusinessErrorFilter(ILogger<BusinessErrorFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        if (exception is FacilityAccessException) { context.Result = new ForbidResult(); context.ExceptionHandled = true; return; }
        var sql = exception as SqlException ?? (exception as DbUpdateException)?.InnerException as SqlException;
        if (exception is not BusinessException && sql is null) return;
        logger.LogWarning(exception, "Business operation rejected; request {RequestId}", context.HttpContext.TraceIdentifier);
        string message = exception is BusinessException ? exception.Message : sql!.Number switch
        {
            >= 51000 and < 51500 => sql.Message,
            2601 or 2627 => "Dữ liệu bị trùng. Kiểm tra tên, mã hoặc khoảng hiệu lực.",
            547 => "Dữ liệu không phù hợp ràng buộc hoặc đang được sử dụng.",
            1205 or -2 => "Dữ liệu đang được thao tác. Hãy tải lại và thử lại.",
            _ => "Không thể kết nối hoặc lưu dữ liệu. Vui lòng thử lại sau."
        };
        context.Result = new ViewResult { ViewName = "~/Views/Shared/BusinessError.cshtml", ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = message }, StatusCode = 400 };
        context.ExceptionHandled = true;
    }
}
