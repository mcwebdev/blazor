namespace FlowBoard.Application.Interfaces;

public interface INotificationUpdateNotifier
{
    Task NotifyUnreadCountChangedAsync(string userId, int unreadCount);
}
