using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class LichDatCoDinh
{
    public long Id { get; set; }

    public long NguoiDungId { get; set; }

    public long SanId { get; set; }

    public int ThuTrongTuan { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public bool TrangThaiKichHoat { get; set; }

    public DateTime NgayTao { get; set; }

    public int KhungGioId { get; set; }

    public DateTime NgayCapNhat { get; set; }

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual KhungGioMacDinh KhungGio { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual San San { get; set; } = null!;
}
