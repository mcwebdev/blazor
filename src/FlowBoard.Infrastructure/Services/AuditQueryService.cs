using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Enums;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class AuditQueryService : IAuditQueryService
{
    private const int PayloadPreviewLength = 220;

    private readonly FlowBoardDbContext _db;

    public AuditQueryService(FlowBoardDbContext db)
    {
        _db = db;
    }

    public async Task<WorkspaceRole?> GetUserRoleAsync(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role;
    }

    public async Task<AuditLogPageDto> GetAuditLogPageAsync(
        Guid workspaceId,
        AuditLogFilterDto filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var needle = filter.Search.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(a =>
                a.Summary.ToLower().Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(filter.EventType)
            && Enum.TryParse<ActivityEventType>(filter.EventType, out var parsedType))
        {
            baseQuery = baseQuery.Where(a => a.EventType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
        {
            baseQuery = baseQuery.Where(a => a.ActorUserId == filter.ActorUserId);
        }

        if (filter.FromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.FromUtc.Value, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(a => a.CreatedAtUtc >= from);
        }

        if (filter.ToUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.ToUtc.Value, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(a => a.CreatedAtUtc <= to);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Project before joining the user table so the SQL stays narrow and
        // EF can translate the take/skip server-side. We resolve display
        // names with a single second roundtrip below.
        var slice = await baseQuery
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.SequenceNumber,
                a.CreatedAtUtc,
                a.ActorUserId,
                a.EventType,
                a.EventCategory,
                a.Summary,
                a.BoardId,
                a.TaskItemId,
                a.IdempotencyKey
            })
            .ToListAsync(cancellationToken);

        var actorIds = slice.Select(s => s.ActorUserId).Distinct().ToArray();
        var actorNames = await _db.Users
            .AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var items = slice
            .Select(a => new AuditLogRowDto(
                a.Id,
                a.SequenceNumber,
                a.CreatedAtUtc,
                actorNames.TryGetValue(a.ActorUserId, out var name) ? name : a.ActorUserId,
                a.ActorUserId,
                a.EventType.ToString(),
                a.EventCategory.ToString(),
                a.Summary,
                a.BoardId,
                a.TaskItemId,
                a.IdempotencyKey))
            .ToList();

        return new AuditLogPageDto(items, totalCount);
    }

    public async Task<IReadOnlyList<WorkspaceMemberRowDto>> GetWorkspaceMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .Join(_db.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (m, u) => new WorkspaceMemberRowDto(
                    u.Id,
                    u.DisplayName,
                    u.Email ?? string.Empty,
                    m.Role,
                    m.JoinedAtUtc))
            .OrderByDescending(r => r.Role)
            .ThenBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<WipBreachRowDto>> GetWipBreachesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        // Project columns with a WipLimit AND a current count that meets or
        // exceeds the limit. Done in a single SQL roundtrip via a join.
        var rows = await _db.BoardColumns
            .AsNoTracking()
            .Where(c => c.WipLimit != null)
            .Join(_db.Boards.AsNoTracking().Where(b => b.WorkspaceId == workspaceId),
                c => c.BoardId,
                b => b.Id,
                (c, b) => new { Column = c, Board = b })
            .Select(x => new
            {
                x.Board.Id,
                BoardName = x.Board.Name,
                ColumnId = x.Column.Id,
                ColumnName = x.Column.Name,
                WipLimit = x.Column.WipLimit!.Value,
                CurrentCount = _db.TaskItems.Count(t => t.ColumnId == x.Column.Id)
            })
            .Where(x => x.CurrentCount >= x.WipLimit)
            .OrderByDescending(x => x.CurrentCount - x.WipLimit)
            .Select(x => new WipBreachRowDto(
                x.Id,
                x.BoardName,
                x.ColumnId,
                x.ColumnName,
                x.WipLimit,
                x.CurrentCount))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<FailedCommandRowDto>> GetFailedCommandsAsync(
        Guid workspaceId,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.FailedCommands
            .AsNoTracking()
            .Where(f => f.WorkspaceId == workspaceId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(take)
            .Select(f => new FailedCommandRowDto(
                f.Id,
                f.CreatedAtUtc,
                f.UserId,
                f.CommandType,
                f.ErrorSummary,
                f.CorrelationId,
                f.PayloadJson.Length > PayloadPreviewLength
                    ? f.PayloadJson.Substring(0, PayloadPreviewLength) + "…"
                    : f.PayloadJson))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task RecordFailedCommandAsync(
        Guid workspaceId,
        string userId,
        string commandType,
        string payloadJson,
        string errorSummary,
        Guid? correlationId,
        CancellationToken cancellationToken = default)
    {
        _db.FailedCommands.Add(new FailedCommand
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            CommandType = commandType,
            PayloadJson = payloadJson,
            ErrorSummary = errorSummary,
            CorrelationId = correlationId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
