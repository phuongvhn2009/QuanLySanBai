using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class VaiTro
{
    public int Id { get; set; }

    public string MaVaiTro { get; set; } = null!;

    public string TenVaiTro { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
}
