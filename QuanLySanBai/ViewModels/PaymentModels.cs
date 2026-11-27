using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using QuanLySanBai.Models.Entities;

namespace QuanLySanBai.ViewModels;

public class SePayOptions
{
    public string BankCode { get; set; } = "MBBank";
    public string BankName { get; set; } = "MB Bank";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string WebhookApiKey { get; set; } = "";
    public string QrBaseUrl { get; set; } = "https://qr.sepay.vn/img";
    public int QrExpireMinutes { get; set; } = 15;
    public string Template { get; set; } = "compact";
    public bool AllowTestWithoutSecrets { get; set; } = false;
}

public class SePayWebhookPayload
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("gateway")]
    public string? Gateway { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("subAccount")]
    public string? SubAccount { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("transferType")]
    public string? TransferType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("accumulated")]
    public decimal? Accumulated { get; set; }

    [JsonPropertyName("referenceCode")]
    public string? ReferenceCode { get; set; }
}

public class TaoThanhToanVM
{
    public long OrderId { get; set; }
    public string MaDatSan { get; set; } = "";
    public string TenCoSo { get; set; } = "";
    public string TenSan { get; set; } = "";
    public DateOnly NgaySuDung { get; set; }
    public TimeOnly GioBatDau { get; set; }
    public TimeOnly GioKetThuc { get; set; }
    public string TrangThaiDon { get; set; } = "";
    public decimal TongTienSan { get; set; }
    public decimal TongTienDichVu { get; set; }
    public decimal TongPhiPhat { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongTienPhaiTra { get; set; }
    public decimal TienCocYeuCau { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConPhaiThu => Math.Max(0m, TongTienPhaiTra - DaThanhToan);
    public decimal SoDuViKhaDung { get; set; }
    public bool IsStaff { get; set; }
    public bool CanCollectCash { get; set; }
    public bool SePayConfigured { get; set; }
    public string Version { get; set; } = "";
    public DateTime? ThoiGianHetHanGiuCho { get; set; }
}

public class CreateQrForm
{
    public long OrderId { get; set; }
    public string Purpose { get; set; } = "COC";
}

public class PayWalletForm
{
    public long OrderId { get; set; }
    public string Version { get; set; } = "";
    public string Purpose { get; set; } = "COC";
    [Required] public string Key { get; set; } = Guid.NewGuid().ToString("N");
}

public class ThanhToanQrVM
{
    public long GatewayTxId { get; set; }
    public long? OrderId { get; set; }
    public string? MaDatSan { get; set; }
    public bool IsWalletDeposit { get; set; }
    public string PaymentCode { get; set; } = "";
    public string TransferContent { get; set; } = "";
    public string QrImageUrl { get; set; } = "";
    public string BankName { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime ServerNow { get; set; }
    public bool IsExpired => Status == "KHOI_TAO" && ExpiresAt <= ServerNow;
    public long? PaymentId { get; set; }
    public bool SePayConfigured { get; set; }
    public string? WarningMessage { get; set; }
    public string? TenCoSo { get; set; }
    public string? TenSan { get; set; }
    public string? TenLoaiSan { get; set; }
    public DateOnly? NgaySuDung { get; set; }
    public TimeOnly? GioBatDau { get; set; }
    public TimeOnly? GioKetThuc { get; set; }
    public decimal TongTienPhaiTra { get; set; }
    public decimal TienCocYeuCau { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConLaiSauGiaoDich => Math.Max(0m, TongTienPhaiTra - DaThanhToan - (Status == "THANH_CONG" ? 0m : Amount));
}

public class ThanhToanTienMatVM
{
    public long OrderId { get; set; }
    public string Version { get; set; } = "";
    [Required] public string Key { get; set; } = Guid.NewGuid().ToString("N");
    [Range(0.01, 999999999999.99)] public decimal Amount { get; set; }
    [Range(0.01, 999999999999.99)] public decimal CashReceived { get; set; }
    public string Purpose { get; set; } = "THANH_TOAN";
}

public class KetQuaThanhToanVM
{
    public long PaymentId { get; set; }
    public string MaThanhToan { get; set; } = "";
    public long OrderId { get; set; }
    public string MaDatSan { get; set; } = "";
    public string TenCoSo { get; set; } = "";
    public string DiaChiCoSo { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string SdtKhachHang { get; set; } = "";
    public string TenSan { get; set; } = "";
    public DateOnly NgaySuDung { get; set; }
    public TimeOnly GioBatDau { get; set; }
    public TimeOnly GioKetThuc { get; set; }
    public string PhuongThuc { get; set; } = "";
    public string MucDich { get; set; } = "";
    public decimal TongTienSan { get; set; }
    public decimal TongPhiPhat { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongTienPhaiTra { get; set; }
    public decimal SoTienThanhToan { get; set; }
    public decimal? SoTienKhachDua { get; set; }
    public decimal? TienThua { get; set; }
    public decimal TongDaThu { get; set; }
    public decimal ConPhaiThu => Math.Max(0m, TongTienPhaiTra - TongDaThu);
    public string? TenNhanVienThu { get; set; }
    public string? MaThamChieuNgoai { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string TrangThaiDon { get; set; } = "";
}

public class WebhookProcessResult
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string ProcessingStatus { get; set; } = "";
    public string Message { get; set; } = "";
    public long? OrderId { get; set; }
    public long? UserId { get; set; }
    public long? CourtId { get; set; }
    public long? GatewayTxId { get; set; }
}

public class WalletIndexVM
{
    public ViNguoiDung Wallet { get; set; } = null!;
    public List<GiaoDichVi> Transactions { get; set; } = [];
    public List<NapTienVi> Deposits { get; set; } = [];
    public List<YeuCauRutTien> Withdrawals { get; set; } = [];
    public List<HoanTien> Refunds { get; set; } = [];
    public bool SePayConfigured { get; set; }
    public bool CanWithdraw { get; set; }
}

public class DepositForm
{
    [Range(1000, 100000000)]
    public decimal Amount { get; set; }
}

public class WithdrawForm
{
    [Range(10000, 100000000)]
    public decimal Amount { get; set; }
    [Required, StringLength(100)]
    public string BankName { get; set; } = "";
    [Required, StringLength(50)]
    public string AccountNumber { get; set; } = "";
    [Required, StringLength(100)]
    public string AccountHolder { get; set; } = "";
}

public class CancelPreviewVM
{
    public long OrderId { get; set; }
    public string MaDatSan { get; set; } = "";
    public decimal TotalPaid { get; set; }
    public decimal HoursBeforeStart { get; set; }
    public decimal PenaltyPercent { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public bool WillRefundToWallet { get; set; }
    public string PolicyName { get; set; } = "";
}

public class RescheduleSlotOption
{
    public long CourtId { get; set; }
    public string CourtName { get; set; } = "";
    public int SlotId { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public decimal NewPrice { get; set; }
}

public class ReschedulePreviewVM
{
    public DonDatSan Order { get; set; } = null!;
    public ChiTietDatSan CurrentSlot { get; set; } = null!;
    public DateOnly SelectedDate { get; set; }
    public List<RescheduleSlotOption> AvailableSlots { get; set; } = [];
    public decimal ChangeFeePercent { get; set; }
    public decimal ChangeFeeAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool AllowChange { get; set; }
    public string? DisallowReason { get; set; }
    public decimal WalletBalance { get; set; }
}

public class RescheduleForm
{
    public long OrderId { get; set; }
    public string Version { get; set; } = "";
    public long NewCourtId { get; set; }
    public int NewSlotId { get; set; }
    public DateOnly NewDate { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
    public bool PayExtraByWallet { get; set; } = true;
}

public class AdminPayoutListVM
{
    public List<YeuCauRutTien> Withdrawals { get; set; } = [];
    public List<NhatKyWebhook> WebhookLogs { get; set; } = [];
}

public class ProcessWithdrawalForm
{
    public long RequestId { get; set; }
    public bool Approve { get; set; }
    [StringLength(500)] public string? RejectReason { get; set; }
}
