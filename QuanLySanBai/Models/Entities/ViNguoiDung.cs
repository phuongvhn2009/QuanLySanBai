using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ViNguoiDung
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public decimal SoDuKhaDung { get; set; }

    public decimal SoDuDongBang { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<GiaoDichVi> GiaoDichVis { get; set; } = new List<GiaoDichVi>();

    public virtual ICollection<NapTienVi> NapTienVis { get; set; } = new List<NapTienVi>();

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual ICollection<YeuCauRutTien> YeuCauRutTiens { get; set; } = new List<YeuCauRutTien>();
}
