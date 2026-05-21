namespace FlowBoard.Web.Hubs;

public interface INotificationClient
{
    Task NotificationCountChanged(int unreadCount);
}
