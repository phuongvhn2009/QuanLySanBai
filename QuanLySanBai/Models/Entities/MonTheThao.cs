using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class MonTheThao
{
    public int Id { get; set; }

    public string MaMon { get; set; } = null!;

    public string TenMon { get; set; } = null!;

    public string? BieuTuongUrl { get; set; }

    public bool TrangThai { get; set; }

    public virtual ICollection<HoSoTrinhDo> HoSoTrinhDos { get; set; } = new List<HoSoTrinhDo>();

    public virtual ICollection<LoaiSan> LoaiSans { get; set; } = new List<LoaiSan>();

    public virtual ICollection<PhongGhepTran> PhongGhepTrans { get; set; } = new List<PhongGhepTran>();
}
