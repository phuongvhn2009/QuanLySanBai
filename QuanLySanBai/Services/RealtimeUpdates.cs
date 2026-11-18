using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLySanBai.Data;
using QuanLySanBai.ViewModels;

namespace QuanLySanBai.Services;

// Public groups carry invalidation signals only, never customer or order data.
public class UpdatesHub(QuanLySanBaiContext db, IOptions<BookingOptions> options) : Hub
{
    public async Task WatchCourt(long courtId)
    {
        if (!await new BookingService(db, options).PublicCourts.AnyAsync(c => c.Id == courtId))
            throw new HubException("Sân không khả dụng.");
        await Groups.AddToGroupAsync(Context.ConnectionId, "court:" + courtId);
    }

    public async Task WatchOrder(long orderId)
    {
        var userId = Context.User is not null ? AccessService.UserId(Context.User) : 0;
        if (userId <= 0)
            throw new HubException("Yêu cầu đăng nhập.");

        var order = await db.DonDatSans.AsNoTracking()
            .Select(d => new { d.Id, d.NguoiDungId, d.CoSoId })
            .SingleOrDefaultAsync(d => d.Id == orderId);
        if (order is null)
            throw new HubException("Không tìm thấy đơn.");

        var access = new AccessService(db);
        if (order.NguoiDungId != userId &&
            !await access.HasPermission(userId, order.CoSoId, "XEM_LICH") &&
            !await access.HasPermission(userId, order.CoSoId, "THU_TIEN"))
            throw new HubException("Không có quyền theo dõi đơn này.");

        await Groups.AddToGroupAsync(Context.ConnectionId, "order:" + orderId);
    }
}

public class RealtimeUpdates(IHubContext<UpdatesHub> hub, ILogger<RealtimeUpdates> logger)
{
    public Task Permissions(long userId) => Send(() => hub.Clients.User(userId.ToString(CultureInfo.InvariantCulture)).SendAsync("PermissionsChanged"));
    public Task Calendar(long courtId) => Send(() => hub.Clients.Group("court:" + courtId).SendAsync("CalendarChanged"));
    public Task PaymentChanged(long? orderId, long? userId) => Send(async () =>
    {
        if (orderId.HasValue)
            await hub.Clients.Group("order:" + orderId.Value).SendAsync("PaymentChanged");
        if (userId.HasValue)
            await hub.Clients.User(userId.Value.ToString(CultureInfo.InvariantCulture)).SendAsync("PaymentChanged");
    });

    private async Task Send(Func<Task> send)
    {
        // A committed booking/payment must remain successful even if notification delivery fails.
        try { await send(); }
        catch (Exception ex) { logger.LogWarning(ex, "Realtime notification failed; clients will refresh on reconnect"); }
    }
}
