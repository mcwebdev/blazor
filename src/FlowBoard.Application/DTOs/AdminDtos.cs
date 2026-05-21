using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.DTOs;

// Mirrors a slice of ActivityLog for the audit grid. We project a flattened
// shape with the actor display name so the grid doesn't have to join.
public sealed record AuditLogRowDto(
    Guid Id,
    long SequenceNumber,
    DateTime CreatedAtUtc,
    string ActorDisplayName,
    string ActorUserId,
    string EventType,
    string EventCategory,
    string Summary,
    Guid? BoardId,
    Guid? TaskItemId,
    string? IdempotencyKey);

// Server-paged result. QuickGrid requires both the slice and the unfiltered
// total so its pager can compute "page X of Y".
public sealed record AuditLogPageDto(
    IReadOnlyList<AuditLogRowDto> Items,
    int TotalCount);

// Filter shape used by QuickGrid's ItemsProvider. Each field is optional;
// null/empty means "no constraint".
public sealed record AuditLogFilterDto(
    string? Search,
    string? EventType,
    string? ActorUserId,
    DateTime? FromUtc,
    DateTime? ToUtc);

public sealed record WorkspaceMemberRowDto(
    string UserId,
    string DisplayName,
    string Email,
    WorkspaceRole Role,
    DateTime JoinedAtUtc);

public sealed record FeatureFlagRowDto(
    Guid Id,
    Guid WorkspaceId,
    string Key,
    string DisplayName,
    string Description,
    bool IsEnabled,
    DateTime UpdatedAtUtc,
    string? UpdatedByUserId);

public sealed record FailedCommandRowDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string UserId,
    string CommandType,
    string ErrorSummary,
    Guid? CorrelationId,
    string PayloadPreview);

// One row per column whose task count meets or exceeds the WIP limit.
public sealed record WipBreachRowDto(
    Guid BoardId,
    string BoardName,
    Guid ColumnId,
    string ColumnName,
    int WipLimit,
    int CurrentCount);
