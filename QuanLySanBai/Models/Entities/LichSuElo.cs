using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichSuElo
{
    public long Id { get; set; }

    public long HoSoTrinhDoId { get; set; }

    public long KetQuaTranDauId { get; set; }

    public int DiemTruoc { get; set; }

    public int DiemThayDoi { get; set; }

    public int DiemSau { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual HoSoTrinhDo HoSoTrinhDo { get; set; } = null!;

    public virtual KetQuaTranDau KetQuaTranDau { get; set; } = null!;
}
