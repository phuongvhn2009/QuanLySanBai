using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ThanhVienCoSo
{
    public long Id { get; set; }

    public long CoSoId { get; set; }

    public long NguoiDungId { get; set; }

    public string VaiTroNoiBo { get; set; } = null!;

    public bool TrangThaiHoatDong { get; set; }

    public DateTime NgayThamGia { get; set; }

    public virtual CoSo CoSo { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<PhanQuyenThanhVien> PhanQuyenThanhViens { get; set; } = new List<PhanQuyenThanhVien>();
}
