using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class DichVu
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public string TenDichVu { get; set; } = null!;

    public string LoaiDichVu { get; set; } = null!;

    public decimal DonGia { get; set; }

    public string DonViTinh { get; set; } = null!;

    public bool ConHang { get; set; }

    public bool DangKinhDoanh { get; set; }

    public virtual ICollection<ChiTietDichVuDon> ChiTietDichVuDons { get; set; } = new List<ChiTietDichVuDon>();

    public virtual CoSo CoSo { get; set; } = null!;
}
