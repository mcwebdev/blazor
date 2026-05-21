using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private static readonly TimeSpan BurndownWindow = TimeSpan.FromDays(14);
    private static readonly TimeSpan WeekWindow = TimeSpan.FromDays(7);

    private static readonly (double UpperDays, string Label)[] CycleBuckets =
    [
        (1, "< 1d"),
        (3, "1–3d"),
        (7, "3–7d"),
        (14, "1–2w"),
        (30, "2–4w"),
        (double.PositiveInfinity, "1m+")
    ];

    private readonly FlowBoardDbContext _db;

    public AnalyticsService(FlowBoardDbContext db)
    {
        _db = db;
    }

    public async Task<BoardAnalyticsSummaryDto?> GetBoardAnalyticsAsync(Guid boardId, CancellationToken ct = default)
    {
        var board = await _db.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => new { b.Id, b.Name })
            .FirstOrDefaultAsync(ct);
        if (board is null)
            return null;

        var now = DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var weekAgo = now - WeekWindow;
        var weekAhead = now + WeekWindow;
        var burndownStart = today.AddDays(-(BurndownWindow.Days - 1));

        var tasksOnBoard = _db.TaskItems.AsNoTracking().Where(t => t.BoardId == boardId);

        var taskShapes = await tasksOnBoard
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.Priority,
                t.AssigneeUserId,
                t.DueDateUtc,
                t.CreatedAtUtc,
                t.CompletedAtUtc
            })
            .ToListAsync(ct);

        var totalTasks = taskShapes.Count;
        var openTasks = taskShapes.Count(t => t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Archived);
        var completedAllTime = taskShapes.Count(t => t.CompletedAtUtc != null);
        var completedLast7Days = taskShapes.Count(t => t.CompletedAtUtc != null && t.CompletedAtUtc >= weekAgo);
        var overdueCount = taskShapes.Count(t =>
            t.DueDateUtc != null
            && t.DueDateUtc < now
            && t.Status != TaskItemStatus.Done
            && t.Status != TaskItemStatus.Archived);
        var dueThisWeek = taskShapes.Count(t =>
            t.DueDateUtc != null
            && t.DueDateUtc >= now
            && t.DueDateUtc <= weekAhead
            && t.Status != TaskItemStatus.Done
            && t.Status != TaskItemStatus.Archived);

        var statusMix = taskShapes
            .GroupBy(t => t.Status)
            .Select(g => new StatusCountDto(g.Key, g.Count()))
            .OrderBy(s => (int)s.Status)
            .ToList();

        var priorityMix = taskShapes
            .GroupBy(t => t.Priority)
            .Select(g => new PriorityCountDto(g.Key, g.Count()))
            .OrderByDescending(p => (int)p.Priority)
            .ToList();

        var assigneeIds = taskShapes
            .Where(t => t.AssigneeUserId != null)
            .Select(t => t.AssigneeUserId!)
            .Distinct()
            .ToList();

        var assigneeNames = await _db.Users
            .AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var workload = taskShapes
            .GroupBy(t => t.AssigneeUserId)
            .Select(g => new WorkloadRowDto(
                g.Key,
                g.Key is null
                    ? "Unassigned"
                    : assigneeNames.TryGetValue(g.Key, out var name) ? name : "Unknown",
                g.Count(t => t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Archived),
                g.Count(t => t.DueDateUtc != null
                    && t.DueDateUtc < now
                    && t.Status != TaskItemStatus.Done
                    && t.Status != TaskItemStatus.Archived),
                g.Count(t => t.CompletedAtUtc != null && t.CompletedAtUtc >= weekAgo)))
            .OrderByDescending(r => r.OpenCount)
            .ThenByDescending(r => r.CompletedLast7Days)
            .ToList();

        var firstMoveByTask = await _db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.BoardId == boardId
                && a.EventType == ActivityEventType.TaskMoved
                && a.TaskItemId != null)
            .GroupBy(a => a.TaskItemId!.Value)
            .Select(g => new
            {
                TaskItemId = g.Key,
                FirstMovedUtc = g.Min(a => a.CreatedAtUtc)
            })
            .ToDictionaryAsync(x => x.TaskItemId, x => x.FirstMovedUtc, ct);

        var leadTimeSamples = new List<double>();
        var cycleTimeSamples = new List<double>();
        foreach (var t in taskShapes)
        {
            if (t.CompletedAtUtc is null)
                continue;

            var lead = (t.CompletedAtUtc.Value - t.CreatedAtUtc).TotalDays;
            if (lead >= 0)
                leadTimeSamples.Add(lead);

            if (firstMoveByTask.TryGetValue(t.Id, out var startedUtc))
            {
                var cycle = (t.CompletedAtUtc.Value - startedUtc).TotalDays;
                if (cycle >= 0)
                    cycleTimeSamples.Add(cycle);
            }
        }

        var averageLead = leadTimeSamples.Count > 0 ? leadTimeSamples.Average() : 0;
        var averageCycle = cycleTimeSamples.Count > 0 ? cycleTimeSamples.Average() : 0;

        var burndown = new List<BurndownPointDto>(BurndownWindow.Days);
        for (var i = 0; i < BurndownWindow.Days; i++)
        {
            var day = burndownStart.AddDays(i);
            var endOfDay = day.AddDays(1);
            var outstanding = taskShapes.Count(t =>
                t.CreatedAtUtc < endOfDay
                && (t.CompletedAtUtc == null || t.CompletedAtUtc >= endOfDay));
            var completedOnDay = taskShapes.Count(t =>
                t.CompletedAtUtc != null
                && t.CompletedAtUtc >= day
                && t.CompletedAtUtc < endOfDay);
            burndown.Add(new BurndownPointDto(day, outstanding, completedOnDay));
        }

        var cycleHistogram = BuildHistogram(cycleTimeSamples);
        var leadHistogram = BuildHistogram(leadTimeSamples);

        return new BoardAnalyticsSummaryDto(
            board.Id,
            board.Name,
            now,
            totalTasks,
            openTasks,
            completedAllTime,
            completedLast7Days,
            overdueCount,
            dueThisWeek,
            Math.Round(averageLead, 2),
            Math.Round(averageCycle, 2),
            cycleTimeSamples.Count,
            statusMix,
            priorityMix,
            workload,
            burndown,
            cycleHistogram,
            leadHistogram);
    }

    public async Task<WorkspaceAnalyticsSummaryDto> GetWorkspaceSummaryAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var boardsRaw = await _db.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId)
            .Select(b => new
            {
                b.Id,
                b.Name,
                ColumnCount = b.Columns.Count
            })
            .ToListAsync(ct);

        var boardIds = boardsRaw.Select(b => b.Id).ToList();

        var tasksRaw = await _db.TaskItems
            .AsNoTracking()
            .Where(t => boardIds.Contains(t.BoardId))
            .Select(t => new
            {
                t.BoardId,
                t.Status,
                t.DueDateUtc,
                t.CompletedAtUtc,
                t.AssigneeUserId
            })
            .ToListAsync(ct);

        var boards = boardsRaw.Select(b => new
        {
            b.Id,
            b.Name,
            b.ColumnCount,
            Tasks = tasksRaw.Where(t => t.BoardId == b.Id)
        }).ToList();

        var now = DateTime.UtcNow;
        var weekAgo = now - WeekWindow;

        var totalBoards = boards.Count;
        var totalTasks = 0;
        var overdueTasks = 0;
        var completedThisWeek = 0;
        var memberIds = new HashSet<string>();
        var boardSummaries = new List<WorkspaceBoardSummaryDto>();

        foreach (var b in boards)
        {
            var bTaskCount = b.Tasks.Count();
            var bOverdue = 0;
            var bCompleted = 0;
            var bCompletedAllTime = 0;

            foreach (var t in b.Tasks)
            {
                if (t.AssigneeUserId != null)
                {
                    memberIds.Add(t.AssigneeUserId);
                }

                if (t.Status == TaskItemStatus.Done || t.Status == TaskItemStatus.Archived)
                {
                    if (t.CompletedAtUtc.HasValue)
                    {
                        bCompletedAllTime++;
                        if (t.CompletedAtUtc >= weekAgo)
                        {
                            bCompleted++;
                        }
                    }
                }
                else if (t.DueDateUtc.HasValue && t.DueDateUtc < now)
                {
                    bOverdue++;
                }
            }

            totalTasks += bTaskCount;
            overdueTasks += bOverdue;
            completedThisWeek += bCompleted;

            boardSummaries.Add(new WorkspaceBoardSummaryDto(
                b.Id,
                b.Name,
                b.ColumnCount,
                bTaskCount,
                bOverdue,
                bCompletedAllTime
            ));
        }

        return new WorkspaceAnalyticsSummaryDto(
            totalBoards,
            totalTasks,
            memberIds.Count,
            overdueTasks,
            completedThisWeek,
            boardSummaries
        );
    }

    public async Task<AnalyticsDrilldownDto?> GetBoardDrilldownAsync(
        Guid boardId,
        AnalyticsMetric metric,
        TaskFilterDto filter,
        CancellationToken ct = default)
    {
        var board = await _db.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => new { b.Id, b.Name })
            .FirstOrDefaultAsync(ct);
        if (board is null)
            return null;

        var (effectiveFilter, title) = ApplyMetricToFilter(metric, filter);
        var query = ApplyFilter(_db.TaskItems.AsNoTracking().Where(t => t.BoardId == boardId), effectiveFilter);

        var rawTasks = await query
            .Select(t => new
            {
                t.Id,
                t.BoardId,
                t.Title,
                t.Status,
                t.Priority,
                t.AssigneeUserId,
                t.DueDateUtc,
                t.CreatedAtUtc,
                t.CompletedAtUtc
            })
            .ToListAsync(ct);

        var assigneeIds = rawTasks
            .Where(t => t.AssigneeUserId != null)
            .Select(t => t.AssigneeUserId!)
            .Distinct()
            .ToList();

        var assigneeNames = await _db.Users
            .AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var firstMoveByTask = await _db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.BoardId == boardId
                && a.EventType == ActivityEventType.TaskMoved
                && a.TaskItemId != null
                && rawTasks.Select(rt => rt.Id).Contains(a.TaskItemId!.Value))
            .GroupBy(a => a.TaskItemId!.Value)
            .Select(g => new { TaskItemId = g.Key, FirstMovedUtc = g.Min(a => a.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.TaskItemId, x => x.FirstMovedUtc, ct);

        var rows = rawTasks
            .OrderByDescending(t => t.CompletedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(t => (int)t.Priority)
            .ThenBy(t => t.DueDateUtc ?? DateTime.MaxValue)
            .Select(t =>
            {
                double? lead = t.CompletedAtUtc.HasValue
                    ? Math.Round((t.CompletedAtUtc.Value - t.CreatedAtUtc).TotalDays, 2)
                    : null;
                double? cycle = null;
                if (t.CompletedAtUtc.HasValue && firstMoveByTask.TryGetValue(t.Id, out var startedUtc))
                {
                    var c = (t.CompletedAtUtc.Value - startedUtc).TotalDays;
                    if (c >= 0)
                        cycle = Math.Round(c, 2);
                }

                return new AnalyticsTaskRowDto(
                    t.Id,
                    t.BoardId,
                    t.Title,
                    t.Status,
                    t.Priority,
                    t.AssigneeUserId,
                    t.AssigneeUserId is null
                        ? null
                        : assigneeNames.TryGetValue(t.AssigneeUserId, out var name) ? name : null,
                    t.DueDateUtc,
                    t.CreatedAtUtc,
                    t.CompletedAtUtc,
                    lead,
                    cycle);
            })
            .ToList();

        return new AnalyticsDrilldownDto(
            board.Id,
            board.Name,
            metric,
            title,
            effectiveFilter,
            rows);
    }

    private static (TaskFilterDto Filter, string Title) ApplyMetricToFilter(AnalyticsMetric metric, TaskFilterDto filter)
    {
        return metric switch
        {
            AnalyticsMetric.Overdue => (filter with { OverdueOnly = true }, "Overdue tasks"),
            AnalyticsMetric.DueThisWeek => (
                filter with
                {
                    DueAfterUtc = filter.DueAfterUtc ?? DateTime.UtcNow,
                    DueBeforeUtc = filter.DueBeforeUtc ?? DateTime.UtcNow.AddDays(7)
                },
                "Tasks due this week"),
            AnalyticsMetric.CompletedThisWeek => (filter with { CompletedInLast7Days = true }, "Completed in the last 7 days"),
            AnalyticsMetric.OpenByAssignee => (filter, BuildAssigneeTitle(filter)),
            AnalyticsMetric.OpenByStatus => (filter, BuildStatusTitle(filter)),
            AnalyticsMetric.OpenByPriority => (filter, BuildPriorityTitle(filter)),
            AnalyticsMetric.CycleTimeSample => (filter with { CompletedInLast7Days = filter.CompletedInLast7Days ?? false }, "Completed tasks (cycle time sample)"),
            AnalyticsMetric.LeadTimeSample => (filter, "Completed tasks (lead time sample)"),
            _ => (filter, "Filtered tasks")
        };
    }

    private static string BuildAssigneeTitle(TaskFilterDto filter)
    {
        if (filter.AssigneeUserIds is { Count: > 0 })
            return $"Open tasks for {filter.AssigneeUserIds.Count} assignee(s)";
        return "Open tasks by assignee";
    }

    private static string BuildStatusTitle(TaskFilterDto filter)
    {
        if (filter.Statuses is { Count: 1 })
            return $"Tasks in {filter.Statuses[0]}";
        return "Tasks by status";
    }

    private static string BuildPriorityTitle(TaskFilterDto filter)
    {
        if (filter.Priorities is { Count: 1 })
            return $"{filter.Priorities[0]} priority tasks";
        return "Tasks by priority";
    }

    private static IQueryable<TaskItem> ApplyFilter(IQueryable<TaskItem> query, TaskFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(t => EF.Functions.Like(t.Title, $"%{s}%")
                || (t.Description != null && EF.Functions.Like(t.Description, $"%{s}%")));
        }

        if (filter.AssigneeUserIds is { Count: > 0 })
        {
            var ids = filter.AssigneeUserIds;
            query = query.Where(t => t.AssigneeUserId != null && ids.Contains(t.AssigneeUserId));
        }

        if (filter.Priorities is { Count: > 0 })
        {
            var priorities = filter.Priorities;
            query = query.Where(t => priorities.Contains(t.Priority));
        }

        if (filter.Statuses is { Count: > 0 })
        {
            var statuses = filter.Statuses;
            query = query.Where(t => statuses.Contains(t.Status));
        }

        if (filter.LabelIds is { Count: > 0 })
        {
            var labelIds = filter.LabelIds;
            query = query.Where(t => t.TaskLabels.Any(tl => labelIds.Contains(tl.TaskLabelId)));
        }

        if (filter.DueAfterUtc.HasValue)
            query = query.Where(t => t.DueDateUtc != null && t.DueDateUtc >= filter.DueAfterUtc);

        if (filter.DueBeforeUtc.HasValue)
            query = query.Where(t => t.DueDateUtc != null && t.DueDateUtc <= filter.DueBeforeUtc);

        if (filter.OverdueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.DueDateUtc != null
                && t.DueDateUtc < now
                && t.Status != TaskItemStatus.Done
                && t.Status != TaskItemStatus.Archived);
        }

        if (filter.CompletedInLast7Days == true)
        {
            var weekAgo = DateTime.UtcNow - WeekWindow;
            query = query.Where(t => t.CompletedAtUtc != null && t.CompletedAtUtc >= weekAgo);
        }

        if (filter.IncludeArchived != true)
            query = query.Where(t => t.Status != TaskItemStatus.Archived);

        return query;
    }

    private static List<CycleTimeBucketDto> BuildHistogram(IReadOnlyList<double> samples)
    {
        var result = new List<CycleTimeBucketDto>(CycleBuckets.Length);
        foreach (var (upper, label) in CycleBuckets)
        {
            result.Add(new CycleTimeBucketDto(label, 0));
        }

        foreach (var sample in samples)
        {
            for (var i = 0; i < CycleBuckets.Length; i++)
            {
                if (sample < CycleBuckets[i].UpperDays)
                {
                    result[i] = result[i] with { Count = result[i].Count + 1 };
                    break;
                }
            }
        }

        return result;
    }
}
