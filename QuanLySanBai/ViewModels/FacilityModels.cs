using System.ComponentModel.DataAnnotations;
using QuanLySanBai.Models.Entities;
namespace QuanLySanBai.ViewModels;

public class FacilityForm
{
    public long Id { get; set; }
    [Required, StringLength(150), Display(Name="Tên cơ sở")] public string Name { get; set; } = "";
    [Required, StringLength(255), Display(Name="Địa chỉ")] public string Address { get; set; } = "";
    [StringLength(20), Display(Name="Hotline")] public string? Phone { get; set; }
    [Display(Name="Giờ mở cửa")] public TimeOnly Open { get; set; } = new(6,0);
    [Display(Name="Giờ đóng cửa (00:00 = 24:00)")] public TimeOnly Close { get; set; } = new(22,0);
    [Display(Name="Đang hoạt động")] public bool Active { get; set; } = true;
}
public class CourtForm
{
    public long Id { get; set; }
    public long FacilityId { get; set; }
    [Required, StringLength(100), Display(Name="Tên sân")] public string Name { get; set; } = "";
    [Range(1,int.MaxValue), Display(Name="Loại sân")] public int TypeId { get; set; }
    [Display(Name="Đang hoạt động")] public bool Active { get; set; } = true;
    [Display(Name="Đang bảo trì")] public bool Maintenance { get; set; }
}
public class HoursForm
{
    public long FacilityId { get; set; }
    [Range(1,7), Display(Name="Thứ (Hai = 1, Chủ nhật = 7)")] public int Weekday { get; set; } = 1;
    public TimeOnly Open { get; set; } = new(6,0);
    public TimeOnly Close { get; set; } = new(22,0);
    [Display(Name="Nghỉ cả ngày")] public bool Closed { get; set; }
}
public class SlotForm
{
    public int Id { get; set; }
    public long FacilityId { get; set; }
    public TimeOnly Start { get; set; } = new(6,0);
    public TimeOnly End { get; set; } = new(7,0);
}
public class PriceForm
{
    public long FacilityId { get; set; }
    public long CourtId { get; set; }
    public int SlotId { get; set; }
    [Range(1,7)] public int Weekday { get; set; } = 1;
    [Range(typeof(decimal),"0","999999999999.99")] public decimal Price { get; set; }
    [Range(typeof(decimal),"0","999999999999.99")] public decimal Deposit { get; set; }
    public DateOnly From { get; set; } = VietnamDate();
    public DateOnly? Until { get; set; }
    private static DateOnly VietnamDate() => Services.VietnamTime.Today;
}
public class FeeForm
{
    public decimal From { get; set; }
    public decimal? Until { get; set; }
    public decimal CancelPercent { get; set; }
    public decimal ChangePercent { get; set; }
}
public class PolicyForm
{
    public long FacilityId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    public DateOnly From { get; set; } = Services.VietnamTime.Today;
    public DateOnly? Until { get; set; }
    public bool AllowChange { get; set; }
    public List<FeeForm> Fees { get; set; } = [new()];
}
public class LockForm
{
    public long FacilityId { get; set; }
    public long CourtId { get; set; }
    public DateTime Start { get; set; } = Services.VietnamTime.Now;
    public DateTime End { get; set; } = Services.VietnamTime.Now.AddHours(1);
    [Required, StringLength(500)] public string Reason { get; set; } = "";
}
public class FacilityDashboard
{
    public CoSo Facility { get; set; } = null!;
    public List<San> Courts { get; set; } = [];
    public List<KhungGioMacDinh> Slots { get; set; } = [];
    public List<LichHoatDongCoSo> Hours { get; set; } = [];
    public List<BangGium> Prices { get; set; } = [];
    public List<ChinhSachHuyDoi> Policies { get; set; } = [];
    public List<LichKhoaSan> Locks { get; set; } = [];
}
