using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class KetQuaTranDau
{
    public long Id { get; set; }

    public long PhongGhepId { get; set; }

    public string TiSo { get; set; } = null!;

    public int? SoDoiThang { get; set; }

    public long? NguoiXacNhanDoi1Id { get; set; }

    public long? NguoiXacNhanDoi2Id { get; set; }

    public string TrangThaiXacNhan { get; set; } = null!;

    public DateTime NgayGhiNhan { get; set; }

    public DateTime? NgayXacNhanDoi1 { get; set; }

    public DateTime? NgayXacNhanDoi2 { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<LichSuElo> LichSuElos { get; set; } = new List<LichSuElo>();

    public virtual NguoiDung? NguoiXacNhanDoi1 { get; set; }

    public virtual NguoiDung? NguoiXacNhanDoi2 { get; set; }

    public virtual PhongGhepTran PhongGhep { get; set; } = null!;
}
