using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ChiTietDatSan
{
    public long Id { get; set; }

    public long DonDatSanId { get; set; }

    public long SanId { get; set; }

    public DateOnly NgaySuDung { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public decimal GiaThue { get; set; }

    public string TrangThaiSlot { get; set; } = null!;

    public int KhungGioId { get; set; }

    public long BangGiaId { get; set; }

    public long? LichDatCoDinhId { get; set; }

    public long? ChiTietThayTheChoId { get; set; }

    public long ChinhSachHuyDoiId { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual BangGium BangGia { get; set; } = null!;

    public virtual ChiTietDatSan? ChiTietThayTheCho { get; set; }

    public virtual ChinhSachHuyDoi ChinhSachHuyDoi { get; set; } = null!;

    public virtual DonDatSan DonDatSan { get; set; } = null!;

    public virtual ChiTietDatSan? InverseChiTietThayTheCho { get; set; }

    public virtual KhungGioMacDinh KhungGio { get; set; } = null!;

    public virtual LichDatCoDinh? LichDatCoDinh { get; set; }

    public virtual ICollection<LichSuCheckIn> LichSuCheckIns { get; set; } = new List<LichSuCheckIn>();

    public virtual LichSuHuySan? LichSuHuySan { get; set; }

    public virtual PhongGhepTran? PhongGhepTran { get; set; }

    public virtual San San { get; set; } = null!;

    public virtual YeuCauDoiLich? YeuCauDoiLichChiTietDatSanCu { get; set; }

    public virtual YeuCauDoiLich? YeuCauDoiLichChiTietDatSanMoi { get; set; }
}
