using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichSuCheckIn
{
    public long Id { get; set; }

    public long ChiTietDatSanId { get; set; }

    public long NhanVienCheckinId { get; set; }

    public DateTime ThoiGianCheckin { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual ChiTietDatSan ChiTietDatSan { get; set; } = null!;

    public virtual NguoiDung NhanVienCheckin { get; set; } = null!;
}
