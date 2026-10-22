using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class YeuCauDoiLich
{
    public long Id { get; set; }

    public long ChiTietDatSanCuId { get; set; }

    public long SanMoiId { get; set; }

    public DateOnly NgaySuDungMoi { get; set; }

    public TimeOnly GioBatDauMoi { get; set; }

    public TimeOnly GioKetThucMoi { get; set; }

    public decimal PhiDoiLich { get; set; }

    public decimal ChenhLechGia { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? LyDo { get; set; }

    public DateTime NgayTao { get; set; }

    public long? NguoiYeuCauId { get; set; }

    public long? NguoiXuLyId { get; set; }

    public int KhungGioMoiId { get; set; }

    public long? ChiTietDatSanMoiId { get; set; }

    public DateTime? NgayXuLy { get; set; }

    public virtual ChiTietDatSan ChiTietDatSanCu { get; set; } = null!;

    public virtual ChiTietDatSan? ChiTietDatSanMoi { get; set; }

    public virtual ICollection<HoanTien> HoanTiens { get; set; } = new List<HoanTien>();

    public virtual KhungGioMacDinh KhungGioMoi { get; set; } = null!;

    public virtual NguoiDung? NguoiXuLy { get; set; }

    public virtual NguoiDung? NguoiYeuCau { get; set; }

    public virtual San SanMoi { get; set; } = null!;
}
