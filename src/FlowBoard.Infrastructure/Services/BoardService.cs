using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class BoardService : IBoardService
{
    private readonly FlowBoardDbContext _db;

    public BoardService(FlowBoardDbContext db)
    {
        _db = db;
    }

    public async Task<BoardDto?> GetBoardAsync(Guid boardId)
    {
        var board = await _db.Boards
            .Include(b => b.Columns.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.SortOrder))
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
}
