using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ThongBao
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string LoaiThongBao { get; set; } = null!;

    public long? MaThamChieuId { get; set; }

    public bool DaDoc { get; set; }

    public DateTime NgayTao { get; set; }

    public string? LoaiThamChieu { get; set; }

    public DateTime? NgayDoc { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
