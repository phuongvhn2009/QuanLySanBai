using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class VCongNoDon
{
    public long DonDatSanId { get; set; }

    public string MaDatSan { get; set; } = null!;

    public long CoSoId { get; set; }

    public long? NguoiDungId { get; set; }

    public decimal? TongTienPhaiTra { get; set; }

    public decimal TongDaThu { get; set; }

    public decimal TongDaHoan { get; set; }

    public decimal? SoTienConPhaiThu { get; set; }
}
