using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ChinhSachHuyDoi
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public bool ChoPhepDoiLich { get; set; }

    public string? GhiChu { get; set; }

    public DateOnly NgayHieuLucTu { get; set; }

    public DateOnly? NgayHieuLucDen { get; set; }

    public DateTime NgayTao { get; set; }

    public string TenChinhSach { get; set; } = null!;

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual ICollection<MucPhiHuyDoi> MucPhiHuyDois { get; set; } = new List<MucPhiHuyDoi>();
}
