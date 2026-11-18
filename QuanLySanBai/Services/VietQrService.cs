using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

public class VietQrService(IOptions<SePayOptions> options)
{
    public SePayOptions Settings => options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Settings.BankCode) &&
        !string.IsNullOrWhiteSpace(Settings.AccountNumber);

    public bool IsWebhookConfigured => !string.IsNullOrWhiteSpace(Settings.WebhookApiKey);

    public string BuildQrUrl(decimal amount, string paymentCode)
    {
        if (amount <= 0)
            throw new BusinessException("Số tiền tạo mã QR phải lớn hơn 0.");

        var cleanCode = SanitizeTransferContent(paymentCode);
        if (string.IsNullOrWhiteSpace(cleanCode))
            throw new BusinessException("Mã nội dung chuyển khoản không hợp lệ.");

        if (!IsConfigured && !Settings.AllowTestWithoutSecrets)
            throw new BusinessException("Chưa cấu hình tài khoản ngân hàng nhận tiền (SePay:AccountNumber / SePay:BankCode).");

        var baseUrl = string.IsNullOrWhiteSpace(Settings.QrBaseUrl)
            ? "https://qr.sepay.vn/img"
            : Settings.QrBaseUrl.TrimEnd('/');

        var bank = Uri.EscapeDataString((Settings.BankCode ?? "MBBank").Trim());
        var acc = Uri.EscapeDataString((Settings.AccountNumber ?? "").Trim());
        var amt = amount == Math.Truncate(amount)
            ? ((long)amount).ToString(CultureInfo.InvariantCulture)
            : amount.ToString("0.##", CultureInfo.InvariantCulture);
        var des = Uri.EscapeDataString(cleanCode);
        var template = Uri.EscapeDataString(string.IsNullOrWhiteSpace(Settings.Template) ? "compact" : Settings.Template.Trim());

        return $"{baseUrl}?acc={acc}&bank={bank}&amount={amt}&des={des}&template={template}";
    }

    public static string SanitizeTransferContent(string content)
    {
        var clean = Regex.Replace(content ?? "", @"[^A-Za-z0-9]", "").ToUpperInvariant();
        return clean.Length > 50 ? clean[..50] : clean;
    }
}
