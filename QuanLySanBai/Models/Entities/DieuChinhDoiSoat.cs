using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class DieuChinhDoiSoat
{
    public long Id { get; set; }

    public long KyDoiSoatId { get; set; }

    public long ChiTietDoiSoatGocId { get; set; }

    public long? HoanTienId { get; set; }

    public decimal SoTienDieuChinh { get; set; }

    public string LyDo { get; set; } = null!;

    public long NguoiTaoId { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChiTietDoiSoatDon ChiTietDoiSoatGoc { get; set; } = null!;

    public virtual HoanTien? HoanTien { get; set; }

    public virtual KyDoiSoat KyDoiSoat { get; set; } = null!;

    public virtual NguoiDung NguoiTao { get; set; } = null!;
}
