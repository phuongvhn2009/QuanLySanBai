using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class YeuCauRutTien
{
    public long Id { get; set; }

    public long ViId { get; set; }

    public decimal SoTien { get; set; }

    public string TenNganHang { get; set; } = null!;

    public string SoTaiKhoan { get; set; } = null!;

    public string TenChuTaiKhoan { get; set; } = null!;

    public long? NhanVienDuyetId { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? LyDoTuChoi { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayXuLy { get; set; }

    public long GiaoDichGiuTienId { get; set; }

    public long? GiaoDichKetThucId { get; set; }

    public long? GiaoDichCongId { get; set; }

    public virtual GiaoDichCongThanhToan? GiaoDichCong { get; set; }

    public virtual GiaoDichVi GiaoDichGiuTien { get; set; } = null!;

    public virtual GiaoDichVi? GiaoDichKetThuc { get; set; }

    public virtual NguoiDung? NhanVienDuyet { get; set; }

    public virtual ViNguoiDung Vi { get; set; } = null!;
}
