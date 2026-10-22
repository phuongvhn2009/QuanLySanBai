using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class GiaoDichVi
{
    public long Id { get; set; }

    public long ViId { get; set; }

    public string MaGiaoDich { get; set; } = null!;

    public string LoaiGiaoDich { get; set; } = null!;

    public decimal SoTien { get; set; }

    public decimal SoDuTruoc { get; set; }

    public decimal SoDuSau { get; set; }

    public string? NoiDung { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public decimal SoDuDongBangTruoc { get; set; }

    public decimal SoDuDongBangSau { get; set; }

    public string KhoaChongLap { get; set; } = null!;

    public virtual HoanTien? HoanTien { get; set; }

    public virtual NapTienVi? NapTienVi { get; set; }

    public virtual ThanhToanDon? ThanhToanDon { get; set; }

    public virtual ViNguoiDung Vi { get; set; } = null!;

    public virtual YeuCauRutTien? YeuCauRutTienGiaoDichGiuTien { get; set; }

    public virtual YeuCauRutTien? YeuCauRutTienGiaoDichKetThuc { get; set; }
}
