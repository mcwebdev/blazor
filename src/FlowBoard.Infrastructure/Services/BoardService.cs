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
                    t.SortOrder
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

    public async Task<TaskDetailDto?> GetTaskAsync(Guid taskId)
    {
        var task = await _db.TaskItems
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
            task.RowVersion
        );
    }

    public async Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto taskDto)
    {
        var title = taskDto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Task title is required.");

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
            RowVersion = Guid.NewGuid().ToByteArray()
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
            });

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
        _db.Entry(task).Property(t => t.RowVersion).OriginalValue = taskDto.RowVersion;

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
        task.RowVersion = Guid.NewGuid().ToByteArray();

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
        task.RowVersion = Guid.NewGuid().ToByteArray();

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

    private Task<Guid> GetWorkspaceIdForBoardAsync(Guid boardId)
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
        object after)
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
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
