using FlowBoard.Application.Interfaces;
using FlowBoard.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Services;

public sealed class NotificationUpdateNotifier(IHubContext<NotificationHub, INotificationClient> hubContext) : INotificationUpdateNotifier
{
    public async Task NotifyUnreadCountChangedAsync(string userId, int unreadCount)
    {
        await hubContext.Clients
            .Group(NotificationHub.GetUserGroup(userId))
            .NotificationCountChanged(unreadCount);
    }
}
