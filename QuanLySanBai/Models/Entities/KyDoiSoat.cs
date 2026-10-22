using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class KyDoiSoat
{
    public long Id { get; set; }

    public string MaKyDoiSoat { get; set; } = null!;

    public long CoSoId { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public decimal TongDoanhThuApp { get; set; }

    public decimal TongDoanhThuTaiQuay { get; set; }

    public decimal TongVoucherSanTroGia { get; set; }

    public decimal TongPhiHoaHong { get; set; }

    public decimal SoTienThucChuyen { get; set; }

    public string TrangThai { get; set; } = null!;

    public long? NhanVienDoiSoatId { get; set; }

    public DateTime? NgayChot { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public decimal TongTienHoanApp { get; set; }

    public string? MaChuyenKhoan { get; set; }

    public virtual ICollection<ChiTietDoiSoatDon> ChiTietDoiSoatDons { get; set; } = new List<ChiTietDoiSoatDon>();

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual ICollection<DieuChinhDoiSoat> DieuChinhDoiSoats { get; set; } = new List<DieuChinhDoiSoat>();

    public virtual NguoiDung? NhanVienDoiSoat { get; set; }
}
