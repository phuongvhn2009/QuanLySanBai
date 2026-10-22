using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class NapTienVi
{
    public long Id { get; set; }

    public long ViId { get; set; }

    public long GiaoDichCongId { get; set; }

    public long? GiaoDichViId { get; set; }

    public decimal SoTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime? NgayHoanTat { get; set; }

    public virtual GiaoDichCongThanhToan GiaoDichCong { get; set; } = null!;

    public virtual GiaoDichVi? GiaoDichVi { get; set; }

    public virtual ViNguoiDung Vi { get; set; } = null!;
}
