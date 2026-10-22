using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ChiTietDichVuDon
{
    public long Id { get; set; }

    public long DonDatSanId { get; set; }

    public long DichVuId { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGiaLucBan { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual DichVu DichVu { get; set; } = null!;

    public virtual DonDatSan DonDatSan { get; set; } = null!;
}
