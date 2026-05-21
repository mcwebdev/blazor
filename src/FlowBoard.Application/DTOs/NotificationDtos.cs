using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.DTOs;

// Single notification row projected for the bell dropdown and the
// /notifications page.
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    Guid? RelatedTaskId,
    bool IsRead,
    DateTime CreatedAtUtc);

// Bell-dropdown payload. The unread count comes back with the list so the
// caller doesn't make a second roundtrip after marking one read.
public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);
