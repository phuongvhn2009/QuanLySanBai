using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class PhanQuyenThanhVien
{
    public long Id { get; set; }

    public long ThanhVienCoSoId { get; set; }

    public int QuyenHanId { get; set; }

    public virtual QuyenHan QuyenHan { get; set; } = null!;

    public virtual ThanhVienCoSo ThanhVienCoSo { get; set; } = null!;
}
