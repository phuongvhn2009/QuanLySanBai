using System.ComponentModel.DataAnnotations;
using QuanLySanBai.Models.Entities;
namespace QuanLySanBai.ViewModels;
public class CollectForm
{
    public long Id { get; set; }
    public string Version { get; set; }="";
    [Required] public string Key { get; set; }=Guid.NewGuid().ToString("N");
    public decimal Amount { get; set; }
    public string Method { get; set; }="TIEN_MAT";
    public string Purpose { get; set; }="COC";
    [StringLength(100)] public string? Reference { get; set; }
}
public class OrderActionForm
{
    public long Id { get; set; }
    public string Version { get; set; }="";
    [StringLength(1000)] public string? Reason { get; set; }
    [StringLength(255)] public string? Code { get; set; }
}
public class CounterForm
{
    public long FacilityId { get; set; }
    public long CourtId { get; set; }
    public int SlotId { get; set; }
    public DateOnly Date { get; set; }=Services.VietnamTime.Today.AddDays(1);
    [Required,StringLength(100)] public string Name { get; set; }="";
    [Required,RegularExpression(@"^0[0-9]{9}$")] public string Phone { get; set; }="";
}
public class OperationDetails
{
    public DonDatSan Order { get; set; }=null!;
    public decimal Paid { get; set; }
    public bool Collect { get; set; }
    public bool CheckIn { get; set; }
    public bool Cancel { get; set; }
    public string Version => Convert.ToBase64String(Order.RowVersion);
}
public record OperationList(List<DonDatSan> Orders,long FacilityId,int Page,int TotalPages,DateOnly? Date,string? Status,string? Phone);
public class OperationOptions { public int CheckInEarlyMinutes { get; set; }=15; }
