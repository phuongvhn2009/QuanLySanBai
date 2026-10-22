using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class NhatKyViPham
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public long? DonDatSanId { get; set; }

    public string LoaiViPham { get; set; } = null!;

    public int DiemUyTinBiTru { get; set; }

    public DateTime? KhoaTinhNangDen { get; set; }

    public string NoiDungViPham { get; set; } = null!;

    public DateTime NgayGhiNhan { get; set; }

    public virtual DonDatSan? DonDatSan { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
