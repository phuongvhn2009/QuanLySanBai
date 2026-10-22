using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class CoSo
{
    public long Id { get; set; }

    public long ChuSoHuuId { get; set; }

    public string TenCoSo { get; set; } = null!;

    public string DiaChi { get; set; } = null!;

    public decimal? KinhDo { get; set; }

    public decimal? ViDo { get; set; }

    public string? SoDienThoaiHotline { get; set; }

    public string? AnhDaiDien { get; set; }

    public TimeOnly GioMoCua { get; set; }

    public TimeOnly GioDongCua { get; set; }

    public string? TienIch { get; set; }

    public string TrangThaiDuyet { get; set; } = null!;

    public bool DangHoatDong { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<CauHinhHoaHong> CauHinhHoaHongs { get; set; } = new List<CauHinhHoaHong>();

    public virtual ICollection<ChinhSachHuyDoi> ChinhSachHuyDois { get; set; } = new List<ChinhSachHuyDoi>();

    public virtual NguoiDung ChuSoHuu { get; set; } = null!;

    public virtual ICollection<DichVu> DichVus { get; set; } = new List<DichVu>();

    public virtual ICollection<DonDatSan> DonDatSans { get; set; } = new List<DonDatSan>();

    public virtual ICollection<KhungGioMacDinh> KhungGioMacDinhs { get; set; } = new List<KhungGioMacDinh>();

    public virtual ICollection<KyDoiSoat> KyDoiSoats { get; set; } = new List<KyDoiSoat>();

    public virtual ICollection<LichHoatDongCoSo> LichHoatDongCoSos { get; set; } = new List<LichHoatDongCoSo>();

    public virtual ICollection<San> Sans { get; set; } = new List<San>();

    public virtual ICollection<ThanhVienCoSo> ThanhVienCoSos { get; set; } = new List<ThanhVienCoSo>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
