using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class BoardService : IBoardService
{
    private readonly FlowBoardDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BoardService(FlowBoardDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId)
    {
        var board = await _db.Boards
            .Include(b => b.Columns.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.SortOrder))
                    .ThenInclude(t => t.TaskLabels)
                        .ThenInclude(til => til.TaskLabel)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board is null)
            return null;

        // Build a lookup of user display names for assignees.
        var assigneeIds = board.Columns
            .SelectMany(c => c.Tasks)
            .Where(t => t.AssigneeUserId is not null)
            .Select(t => t.AssigneeUserId!)
            .Distinct()
            .ToList();

        var userNames = await _db.Users
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        return new BoardDto(
            board.Id,
            board.Name,
            board.Description,
            board.WorkspaceId,
            board.Columns.Select(c => new BoardColumnDto(
                c.Id,
                c.Name,
                c.SortOrder,
                c.WipLimit,
                c.Tasks.Select(t => new TaskCardDto(
                    t.Id,
                    t.ColumnId,
                    t.Title,
                    t.Priority,
                    t.Status,
                    t.AssigneeUserId is not null && userNames.TryGetValue(t.AssigneeUserId, out var name) ? name : null,
                    t.AssigneeUserId,
                    t.DueDateUtc,
                    0, // comment count — will be filled when comments are loaded
                    0,
                    0,
                    t.SortOrder,
                    t.TaskLabels.Select(l => new TaskLabelDto(l.TaskLabel.Id, l.TaskLabel.Name, l.TaskLabel.Color)).ToList()
                )).ToList()
            )).ToList()
        );
    }

    public async Task<IReadOnlyList<BoardDto>> GetBoardsForWorkspaceAsync(Guid workspaceId)
    {
        var boards = await _db.Boards
            .Where(b => b.WorkspaceId == workspaceId && !b.IsArchived)
            .OrderBy(b => b.CreatedAtUtc)
            .Select(b => new BoardDto(
                b.Id,
                b.Name,
                b.Description,
                b.WorkspaceId,
                new List<BoardColumnDto>()
            ))
            .AsNoTracking()
            .ToListAsync();

        return boards;
    }

    public async Task<IReadOnlyList<ActivityEntryDto>> GetRecentActivityForBoardAsync(Guid boardId, int take = 12)
    {
        var activity = await _db.ActivityLogs
            .Where(a => a.BoardId == boardId)
            .OrderByDescending(a => a.SequenceNumber)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.Summary,
                a.ActorUserId,
                a.CreatedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var actorIds = activity
            .Select(a => a.ActorUserId)
            .Where(id => id != "system")
            .Distinct()
            .ToList();

        var actorNames = await _db.Users
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        return activity
            .Select(a => new ActivityEntryDto(
                a.Id,
                a.Summary,
                actorNames.TryGetValue(a.ActorUserId, out var actorName)
                    ? actorName
                    : a.ActorUserId == "system" ? "System" : "Unknown",
                a.CreatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<ReplayEventDto>> GetReplayEventsAsync(Guid boardId, DateTime since)
    {
        var events = await _db.ActivityLogs
            .Where(a => a.BoardId == boardId && a.CreatedAtUtc >= since)
            .OrderBy(a => a.SequenceNumber)
            .Select(a => new
            {
                a.Id,
                a.SequenceNumber,
                EventType = a.EventType.ToString(),
                a.Summary,
                a.ActorUserId,
                a.CreatedAtUtc,
                a.BeforeJson,
                a.AfterJson
            })
            .AsNoTracking()
            .ToListAsync();

        var actorIds = events
            .Select(e => e.ActorUserId)
            .Where(id => id != "system")
            .Distinct()
            .ToList();

        var actorNames = await _db.Users
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        return events
            .Select(e => new ReplayEventDto(
                e.Id,
                e.SequenceNumber,
                e.EventType,
                e.Summary,
                actorNames.TryGetValue(e.ActorUserId, out var name)
                    ? name
                    : e.ActorUserId == "system" ? "System" : "Unknown",
                e.CreatedAtUtc,
                e.BeforeJson,
                e.AfterJson))
            .ToList();
    }

    public async Task<IReadOnlyList<BoardMemberDto>> GetBoardMembersAsync(Guid boardId)
    {
        var workspaceId = await _db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => (Guid?)b.WorkspaceId)
            .SingleOrDefaultAsync();

        if (workspaceId is null)
            return [];

        return await _db.WorkspaceMembers
            .Where(wm => wm.WorkspaceId == workspaceId.Value)
            .Join(
                _db.Users,
                member => member.UserId,
                user => user.Id,
                (_, user) => new { user.Id, user.DisplayName })
            .OrderBy(user => user.DisplayName)
            .Select(user => new BoardMemberDto(user.Id, user.DisplayName))
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<bool> UserCanAccessBoardAsync(Guid boardId, string userId)
    {
        return _db.Boards
            .Where(board => board.Id == boardId)
            .AsNoTracking()
            .Join(
                _db.WorkspaceMembers.Where(member => member.UserId == userId),
                board => board.WorkspaceId,
                member => member.WorkspaceId,
                (_, _) => true)
            .AnyAsync();
    }

    public Task<bool> TaskBelongsToBoardAsync(Guid boardId, Guid taskId)
    {
        return _db.TaskItems
            .AsNoTracking()
            .AnyAsync(task => task.Id == taskId && task.BoardId == boardId);
    }

    public async Task<TaskDetailDto?> GetTaskAsync(Guid taskId)
    {
        var task = await _db.TaskItems
            .Include(t => t.ChecklistItems.OrderBy(c => c.SortOrder))
            .Include(t => t.Comments.OrderBy(c => c.CreatedAtUtc))
            .Include(t => t.TaskLabels)
                .ThenInclude(til => til.TaskLabel)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            return null;

        string? assigneeName = null;
        if (task.AssigneeUserId is not null)
        {
            var user = await _db.Users.FindAsync(task.AssigneeUserId);
            assigneeName = user?.DisplayName;
        }

        var commentUserIds = task.Comments.Select(c => c.UserId).Distinct().ToList();
        var commentUserNames = await _db.Users
            .Where(u => commentUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var checklists = task.ChecklistItems.Select(c => new TaskChecklistItemDto(
            c.Id,
            c.Text,
            c.IsComplete,
            c.SortOrder
        )).ToList();

        var comments = task.Comments.Select(c => new TaskCommentDto(
            c.Id,
            c.Body,
            commentUserNames.TryGetValue(c.UserId, out var name) ? name : "Unknown",
            c.CreatedAtUtc
        )).ToList();

        var labels = task.TaskLabels.Select(l => new TaskLabelDto(
            l.TaskLabel.Id,
            l.TaskLabel.Name,
            l.TaskLabel.Color
        )).ToList();

        return new TaskDetailDto(
            task.Id,
            task.BoardId,
            task.ColumnId,
            task.Title,
            task.Description,
            task.Priority,
            task.Status,
            assigneeName,
            task.AssigneeUserId,
            task.DueDateUtc,
            task.Version,
            labels,
            checklists,
            comments
        );
    }

    public async Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto taskDto)
    {
        var title = taskDto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Task title is required.");

        // Idempotency guard: if the offline queue replays the same create
        // after reconnect, return the previously-created task instead of
        // inserting a duplicate.
        if (!string.IsNullOrWhiteSpace(taskDto.ClientRequestId))
        {
            var existingTaskId = await FindExistingTaskByIdempotencyKeyAsync(
                taskDto.ClientRequestId,
                ActivityEventType.TaskCreated);

            if (existingTaskId.HasValue)
            {
                var existing = await GetTaskAsync(existingTaskId.Value);
                if (existing is not null)
                    return existing;
            }
        }

        var board = await _db.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == taskDto.BoardId);
        if (board is null)
            throw new InvalidOperationException("Board not found.");

        var targetColumn = await GetColumnForStatusAsync(taskDto.BoardId, taskDto.Status)
            ?? await _db.BoardColumns
                .Where(c => c.BoardId == taskDto.BoardId)
                .OrderBy(c => c.SortOrder)
                .FirstOrDefaultAsync();
        if (targetColumn is null)
            throw new InvalidOperationException("Board has no columns.");

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            BoardId = taskDto.BoardId,
            ColumnId = targetColumn.Id,
            Title = title,
            Description = NormalizeOptionalText(taskDto.Description),
            Priority = taskDto.Priority,
            Status = taskDto.Status,
            AssigneeUserId = NormalizeOptionalText(taskDto.AssigneeUserId),
            ReporterUserId = _currentUser.UserId ?? "system",
            DueDateUtc = NormalizeDueDate(taskDto.DueDateUtc),
            SortOrder = await GetNextSortOrderAsync(targetColumn.Id),
            CompletedAtUtc = taskDto.Status == TaskItemStatus.Done ? now : null,
            CreatedAtUtc = now,
            Version = Guid.NewGuid()
        };

        _db.TaskItems.Add(task);

        await RecordActivityAsync(
            board.WorkspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskCreated,
            $"Created {task.Title}.",
            new { },
            new
            {
                task.Title,
                task.Priority,
                task.Status,
                task.AssigneeUserId,
                task.DueDateUtc,
                ColumnId = targetColumn.Id,
                ColumnName = targetColumn.Name
            },
            idempotencyKey: NormalizeOptionalText(taskDto.ClientRequestId));

        await _db.SaveChangesAsync();

        return await GetTaskAsync(task.Id)
            ?? throw new InvalidOperationException("Created task could not be loaded.");
    }

    public async Task UpdateTaskAsync(TaskDetailDto taskDto)
    {
        var task = await _db.TaskItems.FindAsync(taskDto.Id);
        if (task is null)
            return;

        var title = taskDto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Task title is required.");

        var beforeTitle = task.Title;
        var beforePriority = task.Priority;
        var beforeStatus = task.Status;
        var beforeAssignee = task.AssigneeUserId;
        var beforeDueDate = task.DueDateUtc;

        // Optimistic concurrency check
        _db.Entry(task).Property(t => t.Version).OriginalValue = taskDto.Version;

        task.Title = title;
        task.Description = NormalizeOptionalText(taskDto.Description);
        task.Priority = taskDto.Priority;
        task.Status = taskDto.Status;
        task.AssigneeUserId = NormalizeOptionalText(taskDto.AssigneeUserId);
        task.DueDateUtc = NormalizeDueDate(taskDto.DueDateUtc);
        task.CompletedAtUtc = taskDto.Status == TaskItemStatus.Done
            ? task.CompletedAtUtc ?? DateTime.UtcNow
            : null;

        var statusColumn = await GetColumnForStatusAsync(task.BoardId, taskDto.Status);
        if (statusColumn is not null && statusColumn.Id != task.ColumnId)
        {
            task.ColumnId = statusColumn.Id;
            task.SortOrder = await GetNextSortOrderAsync(statusColumn.Id);
        }

        task.LastModifiedByUserId = _currentUser.UserId;
        task.UpdatedAtUtc = DateTime.UtcNow;
        task.Version = Guid.NewGuid();

        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskUpdated,
            $"Updated {task.Title}.",
            new
            {
                Title = beforeTitle,
                Priority = beforePriority,
                Status = beforeStatus,
                AssigneeUserId = beforeAssignee,
                DueDateUtc = beforeDueDate
            },
            new
            {
                task.Title,
                task.Priority,
                task.Status,
                task.AssigneeUserId,
                task.DueDateUtc
            });
        
        await _db.SaveChangesAsync();
    }

    public async Task ForceUpdateTaskAsync(TaskDetailDto taskDto)
    {
        var task = await _db.TaskItems.FindAsync(taskDto.Id);
        if (task is null)
            return;

        var title = taskDto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Task title is required.");

        // Use the current Version from DB — no concurrency check needed
        // (We intentionally skip setting OriginalValue on Version)

        task.Title = title;
        task.Description = NormalizeOptionalText(taskDto.Description);
        task.Priority = taskDto.Priority;
        task.Status = taskDto.Status;
        task.AssigneeUserId = NormalizeOptionalText(taskDto.AssigneeUserId);
        task.DueDateUtc = NormalizeDueDate(taskDto.DueDateUtc);
        task.CompletedAtUtc = taskDto.Status == TaskItemStatus.Done
            ? task.CompletedAtUtc ?? DateTime.UtcNow
            : null;

        var statusColumn = await GetColumnForStatusAsync(task.BoardId, taskDto.Status);
        if (statusColumn is not null && statusColumn.Id != task.ColumnId)
        {
            task.ColumnId = statusColumn.Id;
            task.SortOrder = await GetNextSortOrderAsync(statusColumn.Id);
        }

        task.LastModifiedByUserId = _currentUser.UserId;
        task.UpdatedAtUtc = DateTime.UtcNow;
        task.Version = Guid.NewGuid();

        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskUpdated,
            $"Updated {task.Title} (force overwrite).",
            new { },
            new
            {
                task.Title,
                task.Priority,
                task.Status,
                task.AssigneeUserId,
                task.DueDateUtc
            });

        await _db.SaveChangesAsync();
    }

    public async Task MoveTaskAsync(Guid taskId, Guid newColumnId, int newSortOrder)
    {
        var task = await _db.TaskItems
            .Include(t => t.Column)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
            return;

        var targetColumn = await _db.BoardColumns
            .FirstOrDefaultAsync(c => c.Id == newColumnId && c.BoardId == task.BoardId);
        if (targetColumn is null)
            return;

        if (task.ColumnId == newColumnId)
            return;

        var sourceColumnName = task.Column.Name;
        var targetColumnName = targetColumn.Name;
        var targetSortOrder = newSortOrder >= 0
            ? newSortOrder
            : await GetNextSortOrderAsync(newColumnId);
        var targetStatus = MapColumnToStatus(targetColumn.Name);

        task.ColumnId = newColumnId;
        task.SortOrder = targetSortOrder;
        task.Status = targetStatus;
        task.CompletedAtUtc = targetStatus == TaskItemStatus.Done
            ? task.CompletedAtUtc ?? DateTime.UtcNow
            : null;
        task.LastModifiedByUserId = _currentUser.UserId;
        task.UpdatedAtUtc = DateTime.UtcNow;
        task.Version = Guid.NewGuid();

        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskMoved,
            $"Moved {task.Title} from {sourceColumnName} to {targetColumnName}.",
            new { ColumnId = task.Column.Id, ColumnName = sourceColumnName },
            new { ColumnId = targetColumn.Id, ColumnName = targetColumnName });

        await _db.SaveChangesAsync();
    }

    private static TaskItemStatus MapColumnToStatus(string columnName)
    {
        return columnName.Trim().ToLowerInvariant() switch
        {
            "in progress" => TaskItemStatus.InProgress,
            "review" => TaskItemStatus.InReview,
            "done" => TaskItemStatus.Done,
            _ => TaskItemStatus.Open
        };
    }

    private async Task<BoardColumn?> GetColumnForStatusAsync(Guid boardId, TaskItemStatus status)
    {
        var columnNames = GetColumnNameCandidates(status);

        return await _db.BoardColumns
            .Where(c => c.BoardId == boardId && columnNames.Contains(c.Name))
            .OrderBy(c => c.SortOrder)
            .FirstOrDefaultAsync();
    }

    private static string[] GetColumnNameCandidates(TaskItemStatus status)
    {
        return status switch
        {
            TaskItemStatus.InProgress => ["In Progress"],
            TaskItemStatus.InReview => ["Review", "In Review"],
            TaskItemStatus.Done => ["Done"],
            TaskItemStatus.Open => ["Backlog", "Ready", "Open"],
            _ => []
        };
    }

    private async Task<int> GetNextSortOrderAsync(Guid columnId)
    {
        var maxSortOrder = await _db.TaskItems
            .Where(t => t.ColumnId == columnId)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync();

        return maxSortOrder.GetValueOrDefault(-1) + 1;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? NormalizeDueDate(DateTime? value)
    {
        return value is null
            ? null
            : DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
    }

    public Task<Guid> GetWorkspaceIdForBoardAsync(Guid boardId)
    {
        return _db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => b.WorkspaceId)
            .SingleAsync();
    }

    private async Task RecordActivityAsync(
        Guid workspaceId,
        Guid boardId,
        Guid taskId,
        ActivityEventType eventType,
        string summary,
        object before,
        object after,
        string? idempotencyKey = null)
    {
        var maxSequenceNumber = await _db.ActivityLogs
            .Where(a => a.BoardId == boardId)
            .Select(a => (long?)a.SequenceNumber)
            .MaxAsync();

        _db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            BoardId = boardId,
            TaskItemId = taskId,
            EntityType = nameof(TaskItem),
            EntityId = taskId,
            ActorUserId = _currentUser.UserId ?? "system",
            SequenceNumber = maxSequenceNumber.GetValueOrDefault() + 1,
            EventType = eventType,
            EventCategory = ActivityEventCategory.Task,
            Summary = summary,
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(before),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(after),
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    // Look up the entity created by a previous occurrence of the same client
    // request, scoped to (workspace, command type, actor user) per spec §15.
    // Returns the TaskItemId of the original create, or null if there's no
    // prior record.
    private async Task<Guid?> FindExistingTaskByIdempotencyKeyAsync(
        string clientRequestId,
        ActivityEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(clientRequestId))
            return null;

        var actor = _currentUser.UserId ?? "system";

        return await _db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.IdempotencyKey == clientRequestId
                && a.EventType == eventType
                && a.ActorUserId == actor
                && a.TaskItemId != null)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => (Guid?)a.TaskItemId!.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<TaskChecklistItemDto> AddChecklistItemAsync(Guid taskId, string text, string actorUserId)
    {
        var task = await _db.TaskItems.FindAsync(taskId);
        if (task is null)
            throw new InvalidOperationException("Task not found.");

        var maxSortOrder = await _db.ChecklistItems
            .Where(c => c.TaskItemId == taskId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync();

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            Text = text.Trim(),
            IsComplete = false,
            SortOrder = maxSortOrder.GetValueOrDefault(-1) + 1
        };

        _db.ChecklistItems.Add(item);
        task.UpdatedAtUtc = DateTime.UtcNow;

        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskUpdated,
            $"Added checklist item to {task.Title}.",
            new { },
            new { ChecklistItem = item.Text });

        await _db.SaveChangesAsync();

        return new TaskChecklistItemDto(item.Id, item.Text, item.IsComplete, item.SortOrder);
    }

    public async Task ToggleChecklistItemAsync(Guid itemId, bool isComplete, string actorUserId)
    {
        var item = await _db.ChecklistItems
            .Include(c => c.TaskItem)
            .FirstOrDefaultAsync(c => c.Id == itemId);
            
        if (item is null)
            return;

        if (item.IsComplete == isComplete)
            return;

        item.IsComplete = isComplete;
        item.TaskItem.UpdatedAtUtc = DateTime.UtcNow;

        var workspaceId = await GetWorkspaceIdForBoardAsync(item.TaskItem.BoardId);
        await RecordActivityAsync(
            workspaceId,
            item.TaskItem.BoardId,
            item.TaskItem.Id,
            ActivityEventType.TaskUpdated,
            $"Marked checklist item '{item.Text}' as {(isComplete ? "complete" : "incomplete")}.",
            new { },
            new { ChecklistItem = item.Text, IsComplete = isComplete });

        await _db.SaveChangesAsync();
    }

    public async Task<TaskCommentDto> AddCommentAsync(Guid taskId, string body, string actorUserId, string? clientRequestId = null)
    {
        var task = await _db.TaskItems.FindAsync(taskId);
        if (task is null)
            throw new InvalidOperationException("Task not found.");

        var user = await _db.Users.FindAsync(actorUserId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        // Idempotency guard: if the offline queue replays the same comment
        // after reconnect, return the previously-added comment row instead
        // of inserting a duplicate.
        if (!string.IsNullOrWhiteSpace(clientRequestId))
        {
            var existingActivity = await _db.ActivityLogs
                .AsNoTracking()
                .Where(a => a.IdempotencyKey == clientRequestId
                    && a.EventType == ActivityEventType.CommentAdded
                    && a.ActorUserId == actorUserId
                    && a.TaskItemId == taskId)
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new { a.CreatedAtUtc, a.AfterJson })
                .FirstOrDefaultAsync();

            if (existingActivity is not null)
            {
                var prior = await _db.TaskComments
                    .AsNoTracking()
                    .Where(c => c.TaskItemId == taskId
                        && c.UserId == actorUserId
                        && c.CreatedAtUtc == existingActivity.CreatedAtUtc)
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .FirstOrDefaultAsync();

                if (prior is not null)
                    return new TaskCommentDto(prior.Id, prior.Body, user.DisplayName, prior.CreatedAtUtc);
            }
        }

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            UserId = actorUserId,
            Body = body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.TaskComments.Add(comment);
        task.UpdatedAtUtc = DateTime.UtcNow;

        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.CommentAdded,
            $"Commented on {task.Title}.",
            new { },
            new { CommentBody = comment.Body },
            idempotencyKey: NormalizeOptionalText(clientRequestId));

        await _db.SaveChangesAsync();

        return new TaskCommentDto(comment.Id, comment.Body, user.DisplayName, comment.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<TaskLabelDto>> GetLabelsForBoardAsync(Guid boardId)
    {
        var workspaceId = await GetWorkspaceIdForBoardAsync(boardId);
        return await _db.TaskLabels
            .Where(l => l.WorkspaceId == workspaceId)
            .OrderBy(l => l.Name)
            .Select(l => new TaskLabelDto(l.Id, l.Name, l.Color))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task ToggleTaskLabelAsync(Guid taskId, Guid labelId, bool isApplied, string actorUserId)
    {
        var task = await _db.TaskItems.FindAsync(taskId);
        if (task is null) return;

        var label = await _db.TaskLabels.FindAsync(labelId);
        if (label is null) return;

        var existing = await _db.TaskItemLabels
            .FirstOrDefaultAsync(til => til.TaskItemId == taskId && til.TaskLabelId == labelId);

        if (isApplied && existing is null)
        {
            _db.TaskItemLabels.Add(new TaskItemLabel { TaskItemId = taskId, TaskLabelId = labelId });
        }
        else if (!isApplied && existing is not null)
        {
            _db.TaskItemLabels.Remove(existing);
        }
        else
        {
            return; // No change
        }

        task.UpdatedAtUtc = DateTime.UtcNow;
        var workspaceId = await GetWorkspaceIdForBoardAsync(task.BoardId);
        
        await RecordActivityAsync(
            workspaceId,
            task.BoardId,
            task.Id,
            ActivityEventType.TaskUpdated,
            $"{(isApplied ? "Added" : "Removed")} label '{label.Name}' on {task.Title}.",
            new { },
            new { LabelId = label.Id, LabelName = label.Name, IsApplied = isApplied });

        await _db.SaveChangesAsync();
    }
}
