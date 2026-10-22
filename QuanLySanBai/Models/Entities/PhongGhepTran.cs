using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class PhongGhepTran
{
    public long Id { get; set; }

    public string MaPhong { get; set; } = null!;

    public long NguoiTaoId { get; set; }

    public long ChiTietDatSanId { get; set; }

    public int MonTheThaoId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string TheLoai { get; set; } = null!;

    public int DiemEloToiThieu { get; set; }

    public int DiemEloToiDa { get; set; }

    public int SoLuongToiDa { get; set; }

    public decimal TienChiaMoiNguoi { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; }

    public virtual ChiTietDatSan ChiTietDatSan { get; set; } = null!;

    public virtual KetQuaTranDau? KetQuaTranDau { get; set; }

    public virtual MonTheThao MonTheThao { get; set; } = null!;

    public virtual NguoiDung NguoiTao { get; set; } = null!;

    public virtual ICollection<ThanhVienPhong> ThanhVienPhongs { get; set; } = new List<ThanhVienPhong>();
}
