using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class GiaoDichCongThanhToan
{
    public long Id { get; set; }

    public string MaDonHangCong { get; set; } = null!;

    public string? MaGiaoDichNganHang { get; set; }

    public long? NguoiDungId { get; set; }

    public string CongThanhToan { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string LoaiThanhToan { get; set; } = null!;

    public string? DuLieuWebhook { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public string? MaSuKienCong { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual HoanTien? HoanTien { get; set; }

    public virtual NapTienVi? NapTienVi { get; set; }

    public virtual NguoiDung? NguoiDung { get; set; }

    public virtual ThanhToanDon? ThanhToanDon { get; set; }

    public virtual YeuCauRutTien? YeuCauRutTien { get; set; }
}
