using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class ChiTietDoiSoatDon
{
    public long Id { get; set; }

    public long KyDoiSoatId { get; set; }

    public long DonDatSanId { get; set; }

    public decimal TongTienDon { get; set; }

    public decimal TienKhachTraApp { get; set; }

    public decimal TienThuTaiQuay { get; set; }

    public decimal TroGiaSanPhaiTraCoSo { get; set; }

    public decimal PhiHoaHongDon { get; set; }

    public decimal SoTienQuyetToan { get; set; }

    public long CauHinhHoaHongId { get; set; }

    public decimal TiLeHoaHongApDung { get; set; }

    public decimal PhiCoDinhApDung { get; set; }

    public decimal TienHoanApp { get; set; }

    public virtual CauHinhHoaHong CauHinhHoaHong { get; set; } = null!;

    public virtual ICollection<DieuChinhDoiSoat> DieuChinhDoiSoats { get; set; } = new List<DieuChinhDoiSoat>();

    public virtual DonDatSan DonDatSan { get; set; } = null!;

    public virtual KyDoiSoat KyDoiSoat { get; set; } = null!;
}
