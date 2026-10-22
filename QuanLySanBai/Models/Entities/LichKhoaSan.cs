using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichKhoaSan
{
    public long Id { get; set; }

    public long SanId { get; set; }

    public DateTime BatDauLuc { get; set; }

    public DateTime KetThucLuc { get; set; }

    public string? LyDo { get; set; }

    public long NguoiTaoId { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public virtual NguoiDung NguoiTao { get; set; } = null!;

    public virtual San San { get; set; } = null!;
}
