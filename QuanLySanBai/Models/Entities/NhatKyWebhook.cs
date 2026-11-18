using System;

namespace QuanLySanBai.Models.Entities;

public class NhatKyWebhook
{
    public long Id { get; set; }
    public string CongThanhToan { get; set; } = null!;
    public long? SepayTransactionId { get; set; }
    public string? MaThanhToan { get; set; }
    public long? GiaoDichCongId { get; set; }
    public string? LoaiChuyenTien { get; set; }
    public string? SoTaiKhoanNhan { get; set; }
    public decimal? SoTien { get; set; }
    public string? NoiDung { get; set; }
    public string TrangThaiXuLy { get; set; } = null!;
    public string? GhiChu { get; set; }
    public string DuLieuGoc { get; set; } = null!;
    public DateTime NgayTao { get; set; }

    public virtual GiaoDichCongThanhToan? GiaoDichCong { get; set; }
}
