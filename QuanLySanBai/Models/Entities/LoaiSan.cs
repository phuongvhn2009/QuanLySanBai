using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LoaiSan
{
    public int Id { get; set; }

    public int MonTheThaoId { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? KichThuoc { get; set; }

    public int SucChuaNguoi { get; set; }

    public string? MoTa { get; set; }

    public virtual MonTheThao MonTheThao { get; set; } = null!;

    public virtual ICollection<San> Sans { get; set; } = new List<San>();
}
