using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class CauHinhHoaHong
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public decimal TiLeHoaHong { get; set; }

    public decimal PhiCoDinhMoiDon { get; set; }

    public DateOnly NgayApDung { get; set; }

    public bool TrangThai { get; set; }

    public DateOnly? NgayHetHieuLuc { get; set; }

    public virtual ICollection<ChiTietDoiSoatDon> ChiTietDoiSoatDons { get; set; } = new List<ChiTietDoiSoatDon>();

    public virtual CoSo CoSo { get; set; } = null!;
}
