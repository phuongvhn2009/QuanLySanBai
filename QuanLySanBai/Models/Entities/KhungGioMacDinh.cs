using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class KhungGioMacDinh
{
    public int Id { get; set; }

    public long CoSoId { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public bool LaGioVang { get; set; }

    public virtual ICollection<BangGium> BangGia { get; set; } = new List<BangGium>();

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual ICollection<LichDatCoDinh> LichDatCoDinhs { get; set; } = new List<LichDatCoDinh>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLiches { get; set; } = new List<YeuCauDoiLich>();
}
