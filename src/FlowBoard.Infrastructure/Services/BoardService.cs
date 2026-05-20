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

    public async Task UpdateTaskAsync(TaskDetailDto taskDto)
    {
        var task = await _db.TaskItems.FindAsync(taskDto.Id);
        if (task is null)
            return;

        // Optimistic concurrency check
        _db.Entry(task).Property(t => t.RowVersion).OriginalValue = taskDto.RowVersion;

        task.Title = taskDto.Title;
        task.Description = taskDto.Description;
        task.Priority = taskDto.Priority;
        task.Status = taskDto.Status;
        task.AssigneeUserId = taskDto.AssigneeUserId;
        task.DueDateUtc = taskDto.DueDateUtc;

        // In a real app we would set LastModifiedByUserId here as well.
        
        await _db.SaveChangesAsync();
    }

    public async Task MoveTaskAsync(Guid taskId, Guid newColumnId, int newSortOrder)
    {
        var task = await _db.TaskItems.FindAsync(taskId);
        if (task is null)
            return;

        // Simple implementation: just change the column and sort order.
        // In a complete implementation, you'd want to shift the SortOrder of other tasks in the target column 
        // to make room for this one. For now, we'll append/update directly.
        task.ColumnId = newColumnId;
        task.SortOrder = newSortOrder;

        await _db.SaveChangesAsync();
    }
}
