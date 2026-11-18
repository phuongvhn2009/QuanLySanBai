using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class GiaoDichCongThanhToan
{
    public long? DonDatSanId { get; set; }
    public DateTime? ThoiGianHetHanQr { get; set; }
    public string? NoiDungChuyenKhoan { get; set; }
    public long? SepayTransactionId { get; set; }

    public virtual DonDatSan? DonDatSan { get; set; }
    public virtual ICollection<NhatKyWebhook> NhatKyWebhooks { get; set; } = new List<NhatKyWebhook>();
}

public partial class ThanhToanDon
{
    public decimal? SoTienKhachDua { get; set; }
    public decimal? TienThua { get; set; }
}

public partial class DonDatSan
{
    public virtual ICollection<GiaoDichCongThanhToan> GiaoDichCongThanhToans { get; set; } = new List<GiaoDichCongThanhToan>();
}
