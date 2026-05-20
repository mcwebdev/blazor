using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto?> GetBoardAsync(Guid boardId);
    Task<IReadOnlyList<BoardDto>> GetBoardsForWorkspaceAsync(Guid workspaceId);
    Task<TaskDetailDto?> GetTaskAsync(Guid taskId);
    Task UpdateTaskAsync(TaskDetailDto task);
    Task MoveTaskAsync(Guid taskId, Guid newColumnId, int newSortOrder);
}
