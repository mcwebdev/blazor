using System.Text.RegularExpressions;
using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public partial class NotificationService : INotificationService
{
    // Matches @handle tokens in comment bodies. We accept letters, digits,
    // underscores, dots, and hyphens — wide enough for display names like
    // "matt.charlton" and Identity user names like "demo-user".
    [GeneratedRegex(@"(?:^|[\s,;:!?])@([A-Za-z0-9][A-Za-z0-9._-]{1,40})", RegexOptions.Compiled)]
    private static partial Regex MentionPattern();

    private readonly FlowBoardDbContext _db;

    public NotificationService(FlowBoardDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationListDto> GetForUserAsync(
        string userId,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.RelatedTaskId,
                n.IsRead,
                n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var unread = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        return new NotificationListDto(items, unread);
    }

    public Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _db.Notifications.CountAsync(
            n => n.UserId == userId && !n.IsRead,
            cancellationToken);
    }

    public async Task MarkReadAsync(
        Guid notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification is null || notification.IsRead)
            return;

        notification.IsRead = true;
        notification.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyAsync(
        string recipientUserId,
        string? actorUserId,
        NotificationType type,
        string title,
        string body,
        Guid? relatedTaskId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
            return;

        // Self-notifications are noise — assigning yourself to a task you
        // just created shouldn't ping you.
        if (!string.IsNullOrEmpty(actorUserId) && recipientUserId == actorUserId)
            return;

        // Coalesce duplicate assignment/mention notifications targeting
        // the same task to keep the bell from being spammed by rapid
        // edits. If there is already an unread notification of the same
        // type for the same task and user, skip the insert.
        if (relatedTaskId.HasValue)
        {
            var existingUnread = await _db.Notifications
                .AnyAsync(n => n.UserId == recipientUserId
                    && n.RelatedTaskId == relatedTaskId
                    && n.Type == type
                    && !n.IsRead, cancellationToken);

            if (existingUnread)
                return;
        }

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            RelatedTaskId = relatedTaskId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureOverdueRemindersAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Pull every overdue task assigned to this user that isn't Done.
        var overdueTasks = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.AssigneeUserId == userId
                && t.DueDateUtc != null
                && t.DueDateUtc < now
                && t.Status != TaskItemStatus.Done)
            .Select(t => new { t.Id, t.Title, t.DueDateUtc })
            .ToListAsync(cancellationToken);

        if (overdueTasks.Count == 0)
            return;

        var taskIds = overdueTasks.Select(t => t.Id).ToList();

        // Find which of those already have an open (unread) overdue
        // notification. The set we add is the difference.
        var alreadyNotified = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId
                && n.Type == NotificationType.TaskOverdue
                && !n.IsRead
                && n.RelatedTaskId != null
                && taskIds.Contains(n.RelatedTaskId!.Value))
            .Select(n => n.RelatedTaskId!.Value)
            .ToListAsync(cancellationToken);

        var toAdd = overdueTasks
            .Where(t => !alreadyNotified.Contains(t.Id))
            .ToList();

        if (toAdd.Count == 0)
            return;

        foreach (var task in toAdd)
        {
            var days = (int)Math.Max(1, Math.Ceiling((now - task.DueDateUtc!.Value).TotalDays));
            var dayLabel = days == 1 ? "day" : "days";

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.TaskOverdue,
                Title = $"Overdue: {task.Title}",
                Body = $"This task was due {days} {dayLabel} ago.",
                RelatedTaskId = task.Id,
                IsRead = false,
                CreatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    // Service-layer helper: scan a comment body for @handles and return
    // the resolved user ids. Used by BoardService.AddCommentAsync to
    // raise CommentMention notifications.
    public async Task<IReadOnlyList<string>> ResolveMentionedUserIdsAsync(
        Guid boardId,
        string commentBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
            return [];

        var matches = MentionPattern().Matches(commentBody);
        if (matches.Count == 0)
            return [];

        var handles = matches
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToArray();

        // Limit the search to board members so a comment can't ping
        // someone in a different workspace by guessing their handle.
        var memberUserIds = await _db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => _db.Boards.Any(b => b.Id == boardId && b.WorkspaceId == m.WorkspaceId))
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        if (memberUserIds.Count == 0)
            return [];

        var candidates = await _db.Users
            .AsNoTracking()
            .Where(u => memberUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Name = u.UserName ?? string.Empty,
                Display = u.DisplayName,
                Email = u.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        var resolved = new List<string>();
        foreach (var handle in handles)
        {
            var user = candidates.FirstOrDefault(c =>
                c.Name.Equals(handle, StringComparison.OrdinalIgnoreCase) ||
                c.Display.Replace(" ", "").Equals(handle, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Split('@')[0].Equals(handle, StringComparison.OrdinalIgnoreCase));

            if (user is not null && !resolved.Contains(user.Id))
                resolved.Add(user.Id);
        }

        return resolved;
    }
}
