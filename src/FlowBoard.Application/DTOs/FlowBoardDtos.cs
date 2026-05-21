using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.DTOs;

public sealed record TaskCardDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    TaskPriority Priority,
    TaskItemStatus Status,
    string? AssigneeName,
    string? AssigneeUserId,
    DateTime? DueDateUtc,
    int CommentCount,
    int ChecklistCompleteCount,
    int ChecklistTotalCount,
    int SortOrder,
    IReadOnlyList<TaskLabelDto> Labels);

public sealed record TaskChecklistItemDto(
    Guid Id,
    string Text,
    bool IsComplete,
    int SortOrder);

public sealed record TaskLabelDto(
    Guid Id,
    string Name,
    string Color);

public sealed record TaskCommentDto(
    Guid Id,
    string Body,
    string AuthorName,
    DateTime CreatedAtUtc);

public class TaskDetailDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();

    public IReadOnlyList<TaskLabelDto> Labels { get; set; } = [];
    public IReadOnlyList<TaskChecklistItemDto> Checklists { get; set; } = [];
    public IReadOnlyList<TaskCommentDto> Comments { get; set; } = [];

    public TaskDetailDto() { }

    public TaskDetailDto(
        Guid id,
        Guid boardId,
        Guid columnId,
        string title,
        string? description,
        TaskPriority priority,
        TaskItemStatus status,
        string? assigneeName,
        string? assigneeUserId,
        DateTime? dueDateUtc,
        Guid version,
        IReadOnlyList<TaskLabelDto>? labels = null,
        IReadOnlyList<TaskChecklistItemDto>? checklists = null,
        IReadOnlyList<TaskCommentDto>? comments = null)
    {
        Id = id;
        BoardId = boardId;
        ColumnId = columnId;
        Title = title;
        Description = description;
        Priority = priority;
        Status = status;
        AssigneeName = assigneeName;
        AssigneeUserId = assigneeUserId;
        DueDateUtc = dueDateUtc;
        Version = version;
        Labels = labels ?? [];
        Checklists = checklists ?? [];
        Comments = comments ?? [];
    }
}

public class CreateTaskDto
{
    public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Open;
    public string? AssigneeUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }

    // Optional client-supplied key so the offline action queue can replay a
    // create after reconnect without producing duplicate tasks. Scoped to
    // (workspace, user, command-type) at lookup time per spec §15.
    public string? ClientRequestId { get; set; }
}

public sealed record BoardMemberDto(
    string UserId,
    string DisplayName);

public sealed record BoardPresenceDto(
    string UserId,
    string Name);

public sealed record BoardColumnDto(
    Guid Id,
    string Name,
    int SortOrder,
    int? WipLimit,
    IReadOnlyList<TaskCardDto> Tasks);

public sealed record BoardDto(
    Guid Id,
    string Name,
    string? Description,
    Guid WorkspaceId,
    IReadOnlyList<BoardColumnDto> Columns);

public sealed record WorkspaceSummaryDto(
    Guid Id,
    string Name,
    int BoardCount,
    int MemberCount);

public sealed record DashboardSummaryDto(
    int AssignedToMe,
    int DueThisWeek,
    int Overdue,
    int CompletedLast7Days,
    int LiveUsers,
    BoardDto? RecentBoard,
    IReadOnlyList<ActivityEntryDto> RecentActivity);

public sealed record ActivityEntryDto(
    Guid Id,
    string Summary,
    string ActorName,
    DateTime CreatedAtUtc);

public sealed record ReplayEventDto(
    Guid Id,
    long SequenceNumber,
    string EventType,
    string Summary,
    string ActorName,
    DateTime CreatedAtUtc,
    string? BeforeJson,
    string? AfterJson);
