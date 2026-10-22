using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class DonDatSan
{
    public long Id { get; set; }

    public string MaDatSan { get; set; } = null!;

    public long? NguoiDungId { get; set; }

    public long CoSoId { get; set; }

    public long? NhanVienTaoId { get; set; }

    public string NguonDat { get; set; } = null!;

    public decimal? TongTienPhaiTra { get; set; }

    public decimal TienGiamGia { get; set; }

    public string TrangThai { get; set; } = null!;

    public string MaQrCheckin { get; set; } = null!;

    public DateTime? ThoiGianHetHanGiuCho { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public string TenKhachHang { get; set; } = null!;

    public string SdtKhachHang { get; set; } = null!;

    public decimal TongTienSan { get; set; }

    public decimal TongTienDichVu { get; set; }

    public decimal TongPhiPhat { get; set; }

    public decimal TienCocYeuCau { get; set; }

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual ICollection<ChiTietDichVuDon> ChiTietDichVuDons { get; set; } = new List<ChiTietDichVuDon>();

    public virtual ChiTietDoiSoatDon? ChiTietDoiSoatDon { get; set; }

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual DanhGiaCoSo? DanhGiaCoSo { get; set; }

    public virtual LichSuDungVoucher? LichSuDungVoucher { get; set; }

    public virtual NguoiDung? NguoiDung { get; set; }

    public virtual NguoiDung? NhanVienTao { get; set; }

    public virtual ICollection<NhatKyViPham> NhatKyViPhams { get; set; } = new List<NhatKyViPham>();

    public virtual ICollection<ThanhToanDon> ThanhToanDons { get; set; } = new List<ThanhToanDon>();
}
