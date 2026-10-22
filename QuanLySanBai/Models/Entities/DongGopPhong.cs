using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class DongGopPhong
{
    public long Id { get; set; }

    public long ThanhVienPhongId { get; set; }

    public long NguoiGhiNhanId { get; set; }

    public decimal SoTien { get; set; }

    public string LoaiGiaoDich { get; set; } = null!;

    public string PhuongThuc { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public string? MaThamChieu { get; set; }

    public DateTime NgayGiaoDich { get; set; }

    public string? GhiChu { get; set; }

    public virtual NguoiDung NguoiGhiNhan { get; set; } = null!;

    public virtual ThanhVienPhong ThanhVienPhong { get; set; } = null!;
}
