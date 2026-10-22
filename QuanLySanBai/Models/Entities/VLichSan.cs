using System;
using System.Collections.Generic;

namespace QuanLySanBai.Models.Entities;

public partial class VLichSan
{
    public long ChiTietDatSanId { get; set; }

    public long DonDatSanId { get; set; }

    public string MaDatSan { get; set; } = null!;

    public long CoSoId { get; set; }

    public long SanId { get; set; }

    public string TenSan { get; set; } = null!;

    public DateOnly NgaySuDung { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public DateTime? BatDauLuc { get; set; }

    public DateTime? KetThucLuc { get; set; }

    public string TrangThaiSlot { get; set; } = null!;

    public decimal GiaThue { get; set; }

    public DateTime? ThoiGianHetHanGiuCho { get; set; }

    public bool? ChiemLich { get; set; }
}
