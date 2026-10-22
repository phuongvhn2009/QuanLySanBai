using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class VoucherNguoiDung
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public long VoucherId { get; set; }

    public DateTime NgayThuThap { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
