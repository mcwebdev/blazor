using FlowBoard.Application.DTOs;
using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.Interfaces;

public interface INotificationService
{
    // Returns recent notifications plus the unread count in a single
    // payload. Used by the bell dropdown.
    Task<NotificationListDto> GetForUserAsync(
        string userId,
        int take = 20,
        CancellationToken cancellationToken = default);

    // Unread count only. Cheap query for the bell badge polling case.
    Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Mark one notification read. No-op if it belongs to a different user.
    Task MarkReadAsync(
        Guid notificationId,
        string userId,
        CancellationToken cancellationToken = default);

    // Mark every unread notification for this user as read.
    Task MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Internal write API used by service-layer triggers (assignment,
    // mention, overdue). Skips self-notifications (recipient == actor)
    // so users don't ping themselves.
    Task NotifyAsync(
        string recipientUserId,
        string? actorUserId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedTaskId,
        CancellationToken cancellationToken = default);

    // Lazy materialization of overdue reminders. Called at bell-open and
    // at /notifications page load. Idempotent: for each overdue task
    // assigned to the user, ensures at most one unread overdue
    // notification exists per task.
    Task EnsureOverdueRemindersAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // Parses @handles out of a comment body and returns the user ids of
    // board members who match. Used by BoardService.AddCommentAsync to
    // emit CommentMention notifications.
    Task<IReadOnlyList<string>> ResolveMentionedUserIdsAsync(
        Guid boardId,
        string commentBody,
        CancellationToken cancellationToken = default);
}
