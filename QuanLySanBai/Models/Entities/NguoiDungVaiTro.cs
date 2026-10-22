using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class NguoiDungVaiTro
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public int VaiTroId { get; set; }

    public DateTime NgayGan { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual VaiTro VaiTro { get; set; } = null!;
}
