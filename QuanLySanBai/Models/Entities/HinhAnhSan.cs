using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public class HinhAnhSan
{
    public long Id { get; set; }
    public long SanId { get; set; }
    public string DuongDan { get; set; } = null!;
    public bool LaAnhDaiDien { get; set; }
    public int ThuTuHienThi { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public virtual San San { get; set; } = null!;
}

public partial class San
{
    public virtual ICollection<HinhAnhSan> HinhAnhSans { get; set; } = new List<HinhAnhSan>();
}
