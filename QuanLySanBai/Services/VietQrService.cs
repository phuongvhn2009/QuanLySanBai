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

        return BuildQrImageUrl((Settings.BankCode ?? "MBBank").Trim(), (Settings.AccountNumber ?? "").Trim(), amount, cleanCode);
    }

    public string BuildPayoutQrUrl(string bankCodeOrName, string accountNumber, decimal amount, long requestId) =>
        BuildQrImageUrl(NormalizeBankCode(bankCodeOrName), Regex.Replace(accountNumber ?? "", @"[\s\-]", ""), amount, $"QSBP{requestId}");

    private string BuildQrImageUrl(string bankCode, string accountNumber, decimal amount, string cleanCode)
    {
        var baseUrl = string.IsNullOrWhiteSpace(Settings.QrBaseUrl)
            ? "https://qr.sepay.vn/img"
            : Settings.QrBaseUrl.TrimEnd('/');
        var amt = amount == Math.Truncate(amount)
            ? ((long)amount).ToString(CultureInfo.InvariantCulture)
            : amount.ToString("0.##", CultureInfo.InvariantCulture);
        var template = Uri.EscapeDataString(string.IsNullOrWhiteSpace(Settings.Template) ? "compact" : Settings.Template.Trim());
        return $"{baseUrl}?acc={Uri.EscapeDataString(accountNumber)}&bank={Uri.EscapeDataString(bankCode)}&amount={amt}&des={Uri.EscapeDataString(cleanCode)}&template={template}";
    }

    private static string NormalizeBankCode(string? name) => (name ?? "").Trim().ToUpperInvariant() switch
    {
        "MB" or "MB BANK" or "MBBANK" => "MBBank",
        "VCB" or "VIETCOMBANK" => "VCB",
        "TCB" or "TECHCOMBANK" => "TCB",
        "ICB" or "VIETINBANK" or "CTG" => "ICB",
        "BIDV" => "BIDV",
        "ACB" => "ACB",
        "VPB" or "VPBANK" => "VPB",
        "TPB" or "TPBANK" => "TPB",
        "STB" or "SACOMBANK" => "STB",
        _ => (name ?? "").Trim()
    };

    public static string SanitizeTransferContent(string content)
    {
        var clean = Regex.Replace(content ?? "", @"[^A-Za-z0-9]", "").ToUpperInvariant();
        return clean.Length > 50 ? clean[..50] : clean;
    }
}
