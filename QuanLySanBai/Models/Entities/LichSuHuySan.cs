using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichSuHuySan
{
    public long Id { get; set; }

    public long ChiTietDatSanId { get; set; }

    public long? NguoiYeuCauId { get; set; }

    public decimal SoGioTruocTran { get; set; }

    public decimal SoTienDaPhanBo { get; set; }

    public decimal PhiPhatHuy { get; set; }

    public decimal SoTienDuKienHoan { get; set; }

    public string? LyDoHuy { get; set; }

    public DateTime NgayHuy { get; set; }

    public long? NguoiXuLyId { get; set; }

    public virtual ChiTietDatSan ChiTietDatSan { get; set; } = null!;

    public virtual ICollection<HoanTien> HoanTiens { get; set; } = new List<HoanTien>();

    public virtual NguoiDung? NguoiXuLy { get; set; }

    public virtual NguoiDung? NguoiYeuCau { get; set; }
}
