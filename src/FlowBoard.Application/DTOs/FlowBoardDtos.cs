using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.DTOs;

public sealed record TaskCardDto(
    Guid Id,
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
