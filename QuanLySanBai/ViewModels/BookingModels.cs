using System.ComponentModel.DataAnnotations;
using QuanLySanBai.Models.Entities;
namespace QuanLySanBai.ViewModels;
public class BookingForm
{
    public long CourtId { get; set; }
    public int SlotId { get; set; }
    public DateOnly Date { get; set; }
    [StringLength(1000)] public string? Note { get; set; }
}
public record AvailableSlot(int SlotId, TimeOnly Start, TimeOnly End, decimal? Price, decimal? Deposit, string Status, bool CanBook);
public record CourtCalendar(San Court, DateOnly Date, DateTime ServerNow, List<AvailableSlot> Slots, ChinhSachHuyDoi? Policy);
public class BookingOptions
{
    public int HoldMinutes { get; set; }=10;
    public int ExpiryIntervalSeconds { get; set; }=30;
}
