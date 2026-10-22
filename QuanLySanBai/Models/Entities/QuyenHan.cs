using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class QuyenHan
{
    public int Id { get; set; }

    public string MaQuyen { get; set; } = null!;

    public string? NhomChucNang { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<PhanQuyenThanhVien> PhanQuyenThanhViens { get; set; } = new List<PhanQuyenThanhVien>();
}
