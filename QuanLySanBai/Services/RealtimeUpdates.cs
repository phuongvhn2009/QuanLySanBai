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
}

public class RealtimeUpdates(IHubContext<UpdatesHub> hub, ILogger<RealtimeUpdates> logger)
{
    public Task Permissions(long userId) => Send(() => hub.Clients.User(userId.ToString(CultureInfo.InvariantCulture)).SendAsync("PermissionsChanged"));
    public Task Calendar(long courtId) => Send(() => hub.Clients.Group("court:" + courtId).SendAsync("CalendarChanged"));
    private async Task Send(Func<Task> send)
    {
        // A committed booking must remain successful even if notification delivery fails.
        try { await send(); }
        catch (Exception ex) { logger.LogWarning(ex, "Realtime notification failed; clients will refresh on reconnect"); }
    }
}
