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
    int SortOrder);

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
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

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
        byte[] rowVersion)
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
        RowVersion = rowVersion;
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
}

public sealed record BoardMemberDto(
    string UserId,
    string DisplayName);

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
