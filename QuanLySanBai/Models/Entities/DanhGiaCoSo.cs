using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class DanhGiaCoSo
{
    public long Id { get; set; }

    public long DonDatSanId { get; set; }

    public byte SoSao { get; set; }

    public string? NhanXet { get; set; }

    public string? PhanHoiChuSan { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayPhanHoi { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual DonDatSan DonDatSan { get; set; } = null!;
}
