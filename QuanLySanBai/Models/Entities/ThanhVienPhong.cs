using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ThanhVienPhong
{
    public long Id { get; set; }

    public long PhongGhepId { get; set; }

    public long NguoiDungId { get; set; }

    public string VaiTroPhong { get; set; } = null!;

    public string TrangThaiThamGia { get; set; } = null!;

    public DateTime NgayThamGia { get; set; }

    public int? SoDoi { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<DongGopPhong> DongGopPhongs { get; set; } = new List<DongGopPhong>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual PhongGhepTran PhongGhep { get; set; } = null!;
}
