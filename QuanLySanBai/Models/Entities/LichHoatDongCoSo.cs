using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichHoatDongCoSo
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public int ThuTrongTuan { get; set; }

    public TimeOnly GioMoCua { get; set; }

    public TimeOnly GioDongCua { get; set; }

    public bool NghiCaNgay { get; set; }

    public virtual CoSo CoSo { get; set; } = null!;
}
