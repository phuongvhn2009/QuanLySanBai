using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class Voucher
{
    public long Id { get; set; }

    public string MaVoucher { get; set; } = null!;

    public string TieuDe { get; set; } = null!;

    public long? CoSoId { get; set; }

    public string LoaiPhatHanh { get; set; } = null!;

    public string LoaiGiam { get; set; } = null!;

    public decimal GiaTriGiam { get; set; }

    public decimal? GiamToiDa { get; set; }

    public decimal GiaTriDonToiThieu { get; set; }

    public int SoLuongTong { get; set; }

    public int SoLuongDaDung { get; set; }

    public int GioiHanMoiUser { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public bool ChiApDungGioThapDiem { get; set; }

    public bool TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public string BenChiuGiam { get; set; } = null!;

    public virtual CoSo? CoSo { get; set; }

    public virtual ICollection<LichSuDungVoucher> LichSuDungVouchers { get; set; } = new List<LichSuDungVoucher>();

    public virtual ICollection<VoucherNguoiDung> VoucherNguoiDungs { get; set; } = new List<VoucherNguoiDung>();
}
