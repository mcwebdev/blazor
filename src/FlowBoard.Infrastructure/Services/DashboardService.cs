using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly FlowBoardDbContext _db;
    private readonly IBoardService _boardService;

    public DashboardService(FlowBoardDbContext db, IBoardService boardService)
    {
        _db = db;
        _boardService = boardService;
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(string userId, Guid workspaceId)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.AddDays(-7);
        var weekEnd = now.AddDays(7);

        var tasks = _db.TaskItems
            .Where(t => t.Board.WorkspaceId == workspaceId);

        var assignedToMe = await tasks
            .CountAsync(t => t.AssigneeUserId == userId && t.Status != TaskItemStatus.Done);

        var dueThisWeek = await tasks
            .CountAsync(t => t.AssigneeUserId == userId
                && t.DueDateUtc != null
                && t.DueDateUtc >= now
                && t.DueDateUtc <= weekEnd
                && t.Status != TaskItemStatus.Done);

        var overdue = await tasks
            .CountAsync(t => t.DueDateUtc != null
                && t.DueDateUtc < now
                && t.Status != TaskItemStatus.Done);

        var completedLast7Days = await tasks
            .CountAsync(t => t.CompletedAtUtc != null && t.CompletedAtUtc >= weekStart);

        // Get the most recent board in this workspace for the preview.
        var recentBoardId = await _db.Boards
            .Where(b => b.WorkspaceId == workspaceId && !b.IsArchived)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync();

        BoardDto? recentBoard = recentBoardId.HasValue
            ? await _boardService.GetBoardAsync(recentBoardId.Value)
            : null;

        // Recent activity for the workspace.
        var userIds = await _db.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(5)
            .Select(a => a.ActorUserId)
            .Distinct()
            .ToListAsync();

        var userNameMap = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var recentActivity = await _db.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(5)
            .Select(a => new ActivityEntryDto(
                a.Id,
                a.Summary,
                userNameMap.ContainsKey(a.ActorUserId) ? userNameMap[a.ActorUserId] : "Unknown",
                a.CreatedAtUtc
            ))
            .AsNoTracking()
            .ToListAsync();

        return new DashboardSummaryDto(
            assignedToMe,
            dueThisWeek,
            overdue,
            completedLast7Days,
            0, // live users — will come from SignalR presence later
            recentBoard,
            recentActivity
        );
    }
}
