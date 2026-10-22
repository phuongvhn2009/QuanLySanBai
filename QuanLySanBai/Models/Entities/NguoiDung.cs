using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class NguoiDung
{
    public long Id { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public string? Email { get; set; }

    public string MatKhauHash { get; set; } = null!;

    public string HoVaTen { get; set; } = null!;

    public string? AnhDaiDien { get; set; }

    public int DiemUyTin { get; set; }

    public bool BiKhoaGhepTran { get; set; }

    public bool BiKhoaDatSan { get; set; }

    public DateTime? KhoaDoiLichDen { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public DateTime? KhoaGhepTranDen { get; set; }

    public DateTime? KhoaDatSanDen { get; set; }

    public virtual ICollection<CoSo> CoSos { get; set; } = new List<CoSo>();

    public virtual ICollection<DieuChinhDoiSoat> DieuChinhDoiSoats { get; set; } = new List<DieuChinhDoiSoat>();

    public virtual ICollection<DonDatSan> DonDatSanNguoiDungs { get; set; } = new List<DonDatSan>();

    public virtual ICollection<DonDatSan> DonDatSanNhanVienTaos { get; set; } = new List<DonDatSan>();

    public virtual ICollection<DongGopPhong> DongGopPhongs { get; set; } = new List<DongGopPhong>();

    public virtual ICollection<GiaoDichCongThanhToan> GiaoDichCongThanhToans { get; set; } = new List<GiaoDichCongThanhToan>();

    public virtual ICollection<HoSoTrinhDo> HoSoTrinhDos { get; set; } = new List<HoSoTrinhDo>();

    public virtual ICollection<HoanTien> HoanTiens { get; set; } = new List<HoanTien>();

    public virtual ICollection<KetQuaTranDau> KetQuaTranDauNguoiXacNhanDoi1s { get; set; } = new List<KetQuaTranDau>();

    public virtual ICollection<KetQuaTranDau> KetQuaTranDauNguoiXacNhanDoi2s { get; set; } = new List<KetQuaTranDau>();

    public virtual ICollection<KyDoiSoat> KyDoiSoats { get; set; } = new List<KyDoiSoat>();

    public virtual ICollection<LichDatCoDinh> LichDatCoDinhs { get; set; } = new List<LichDatCoDinh>();

    public virtual ICollection<LichKhoaSan> LichKhoaSans { get; set; } = new List<LichKhoaSan>();

    public virtual ICollection<LichSuCheckIn> LichSuCheckIns { get; set; } = new List<LichSuCheckIn>();

    public virtual ICollection<LichSuDungVoucher> LichSuDungVouchers { get; set; } = new List<LichSuDungVoucher>();

    public virtual ICollection<LichSuHuySan> LichSuHuySanNguoiXuLies { get; set; } = new List<LichSuHuySan>();

    public virtual ICollection<LichSuHuySan> LichSuHuySanNguoiYeuCaus { get; set; } = new List<LichSuHuySan>();

    public virtual ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();

    public virtual ICollection<NhatKyViPham> NhatKyViPhams { get; set; } = new List<NhatKyViPham>();

    public virtual ICollection<PhongGhepTran> PhongGhepTrans { get; set; } = new List<PhongGhepTran>();

    public virtual ICollection<ThanhToanDon> ThanhToanDonNguoiThanhToans { get; set; } = new List<ThanhToanDon>();

    public virtual ICollection<ThanhToanDon> ThanhToanDonNhanVienThus { get; set; } = new List<ThanhToanDon>();

    public virtual ICollection<ThanhVienCoSo> ThanhVienCoSos { get; set; } = new List<ThanhVienCoSo>();

    public virtual ICollection<ThanhVienPhong> ThanhVienPhongs { get; set; } = new List<ThanhVienPhong>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ViNguoiDung? ViNguoiDung { get; set; }

    public virtual ICollection<VoucherNguoiDung> VoucherNguoiDungs { get; set; } = new List<VoucherNguoiDung>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLichNguoiXuLies { get; set; } = new List<YeuCauDoiLich>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLichNguoiYeuCaus { get; set; } = new List<YeuCauDoiLich>();

    public virtual ICollection<YeuCauRutTien> YeuCauRutTiens { get; set; } = new List<YeuCauRutTien>();
}
