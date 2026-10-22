using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ThanhToanDon
{
    public long Id { get; set; }

    public string MaThanhToan { get; set; } = null!;

    public long DonDatSanId { get; set; }

    public long? NguoiThanhToanId { get; set; }

    public long? NhanVienThuId { get; set; }

    public long? GiaoDichViId { get; set; }

    public long? GiaoDichCongId { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public string MucDich { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string? MaThamChieuNgoai { get; set; }

    public string KhoaChongLap { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public virtual DonDatSan DonDatSan { get; set; } = null!;

    public virtual GiaoDichCongThanhToan? GiaoDichCong { get; set; }

    public virtual GiaoDichVi? GiaoDichVi { get; set; }

    public virtual ICollection<HoanTien> HoanTiens { get; set; } = new List<HoanTien>();

    public virtual NguoiDung? NguoiThanhToan { get; set; }

    public virtual NguoiDung? NhanVienThu { get; set; }
}
