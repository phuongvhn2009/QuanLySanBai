using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichSuDungVoucher
{
    public long Id { get; set; }

    public long VoucherId { get; set; }

    public long DonDatSanId { get; set; }

    public long NguoiDungId { get; set; }

    public decimal SoTienGiamThucTe { get; set; }

    public DateTime? NgayApDung { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? GiuChoDen { get; set; }

    public DateTime? NgayGiaiToa { get; set; }

    public virtual DonDatSan DonDatSan { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
