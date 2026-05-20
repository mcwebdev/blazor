using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto?> GetBoardAsync(Guid boardId);
    Task<IReadOnlyList<BoardDto>> GetBoardsForWorkspaceAsync(Guid workspaceId);
    Task<IReadOnlyList<ActivityEntryDto>> GetRecentActivityForBoardAsync(Guid boardId, int take = 12);
    Task<IReadOnlyList<BoardMemberDto>> GetBoardMembersAsync(Guid boardId);
    Task<TaskDetailDto?> GetTaskAsync(Guid taskId);
    Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto task);
    Task UpdateTaskAsync(TaskDetailDto task);
    Task MoveTaskAsync(Guid taskId, Guid newColumnId, int newSortOrder);
}
