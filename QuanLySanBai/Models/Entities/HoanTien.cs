using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class HoanTien
{
    public long Id { get; set; }

    public string MaHoanTien { get; set; } = null!;

    public long ThanhToanDonId { get; set; }

    public long? LichSuHuySanId { get; set; }

    public long? YeuCauDoiLichId { get; set; }

    public long? GiaoDichViId { get; set; }

    public long? GiaoDichCongId { get; set; }

    public long? NguoiDuyetId { get; set; }

    public decimal SoTien { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public string? MaThamChieuNgoai { get; set; }

    public string KhoaChongLap { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public string LyDo { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime? NgayHoanTien { get; set; }

    public virtual DieuChinhDoiSoat? DieuChinhDoiSoat { get; set; }

    public virtual GiaoDichCongThanhToan? GiaoDichCong { get; set; }

    public virtual GiaoDichVi? GiaoDichVi { get; set; }

    public virtual LichSuHuySan? LichSuHuySan { get; set; }

    public virtual NguoiDung? NguoiDuyet { get; set; }

    public virtual ThanhToanDon ThanhToanDon { get; set; } = null!;

    public virtual YeuCauDoiLich? YeuCauDoiLich { get; set; }
}
