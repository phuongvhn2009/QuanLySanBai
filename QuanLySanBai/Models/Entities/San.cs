using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class San
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public int LoaiSanId { get; set; }

    public string TenSan { get; set; } = null!;

    public bool LaSanTrongNha { get; set; }

    public string? LoaiMatSan { get; set; }

    public bool DangBaoTri { get; set; }

    public string? GhiChu { get; set; }

    public bool DangHoatDong { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<BangGium> BangGia { get; set; } = new List<BangGium>();

    public virtual ICollection<BaoCaoHieuSuatSan> BaoCaoHieuSuatSans { get; set; } = new List<BaoCaoHieuSuatSan>();

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual ICollection<LichDatCoDinh> LichDatCoDinhs { get; set; } = new List<LichDatCoDinh>();

    public virtual ICollection<LichKhoaSan> LichKhoaSans { get; set; } = new List<LichKhoaSan>();

    public virtual LoaiSan LoaiSan { get; set; } = null!;

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLiches { get; set; } = new List<YeuCauDoiLich>();
}
