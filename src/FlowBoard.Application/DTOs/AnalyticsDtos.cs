using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.DTOs;

public enum AnalyticsMetric
{
    Unspecified = 0,
    Overdue = 1,
    DueThisWeek = 2,
    CompletedThisWeek = 3,
    OpenByAssignee = 4,
    OpenByStatus = 5,
    OpenByPriority = 6,
    CycleTimeSample = 7,
    LeadTimeSample = 8
}

public sealed record TaskFilterDto(
    string? Search = null,
    IReadOnlyList<string>? AssigneeUserIds = null,
    IReadOnlyList<TaskPriority>? Priorities = null,
    IReadOnlyList<TaskItemStatus>? Statuses = null,
    IReadOnlyList<Guid>? LabelIds = null,
    DateTime? DueBeforeUtc = null,
    DateTime? DueAfterUtc = null,
    bool? OverdueOnly = null,
    bool? CompletedInLast7Days = null,
    bool? IncludeArchived = null);

public sealed record StatusCountDto(TaskItemStatus Status, int Count);

public sealed record PriorityCountDto(TaskPriority Priority, int Count);

public sealed record WorkloadRowDto(
    string? AssigneeUserId,
    string AssigneeName,
    int OpenCount,
    int OverdueCount,
    int CompletedLast7Days);

public sealed record BurndownPointDto(DateTime DateUtc, int Outstanding, int Completed);

public sealed record CycleTimeBucketDto(string Label, int Count);

public sealed record BoardAnalyticsSummaryDto(
    Guid BoardId,
    string BoardName,
    DateTime GeneratedAtUtc,
    int TotalTasks,
    int OpenTasks,
    int CompletedAllTime,
    int CompletedLast7Days,
    int OverdueCount,
    int DueThisWeek,
    double AverageLeadTimeDays,
    double AverageCycleTimeDays,
    int CompletedSampleSize,
    IReadOnlyList<StatusCountDto> StatusMix,
    IReadOnlyList<PriorityCountDto> PriorityMix,
    IReadOnlyList<WorkloadRowDto> Workload,
    IReadOnlyList<BurndownPointDto> Burndown,
    IReadOnlyList<CycleTimeBucketDto> CycleTimeHistogram,
    IReadOnlyList<CycleTimeBucketDto> LeadTimeHistogram);

public sealed record AnalyticsTaskRowDto(
    Guid Id,
    Guid BoardId,
    string Title,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssigneeUserId,
    string? AssigneeName,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    double? LeadTimeDays,
    double? CycleTimeDays);

public sealed record AnalyticsDrilldownDto(
    Guid BoardId,
    string BoardName,
    AnalyticsMetric Metric,
    string Title,
    TaskFilterDto Filter,
    IReadOnlyList<AnalyticsTaskRowDto> Tasks);
