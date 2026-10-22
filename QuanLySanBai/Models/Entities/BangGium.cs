using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class BangGium
{
    public long Id { get; set; }

    public long SanId { get; set; }

    public int KhungGioId { get; set; }

    public decimal GiaThue { get; set; }

    public decimal TienCocToiThieu { get; set; }

    public int ThuTrongTuan { get; set; }

    public DateOnly NgayHieuLucTu { get; set; }

    public DateOnly? NgayHieuLucDen { get; set; }

    public bool DangApDung { get; set; }

    public virtual ICollection<ChiTietDatSan> ChiTietDatSans { get; set; } = new List<ChiTietDatSan>();

    public virtual KhungGioMacDinh KhungGio { get; set; } = null!;

    public virtual San San { get; set; } = null!;
}
