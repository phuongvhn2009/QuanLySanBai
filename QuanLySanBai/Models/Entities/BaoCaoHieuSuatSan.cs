using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class BaoCaoHieuSuatSan
{
    public long Id { get; set; }

    public long SanId { get; set; }

    public DateOnly NgayBaoCao { get; set; }

    public int TongSoSlotTrongNgay { get; set; }

    public int SoSlotDaDat { get; set; }

    public int? SoSlotTrong { get; set; }

    public int SoSlotHuy { get; set; }

    public decimal? TyLeLapDay { get; set; }

    public decimal DoanhThuNgay { get; set; }

    public virtual San San { get; set; } = null!;
}
