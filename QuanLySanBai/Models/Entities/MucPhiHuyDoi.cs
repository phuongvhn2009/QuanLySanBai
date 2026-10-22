using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class MucPhiHuyDoi
{
    public long Id { get; set; }

    public long ChinhSachHuyDoiId { get; set; }

    public decimal SoGioTuTruocTran { get; set; }

    public decimal? SoGioDenTruocTran { get; set; }

    public decimal TiLePhiHuy { get; set; }

    public decimal TiLePhiDoi { get; set; }

    public virtual ChinhSachHuyDoi ChinhSachHuyDoi { get; set; } = null!;
}
