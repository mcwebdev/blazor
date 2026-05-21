using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto?> GetBoardAsync(Guid boardId);
    Task<IReadOnlyList<BoardDto>> GetBoardsForWorkspaceAsync(Guid workspaceId);
    Task<IReadOnlyList<ActivityEntryDto>> GetRecentActivityForBoardAsync(Guid boardId, int take = 12);
    Task<IReadOnlyList<BoardMemberDto>> GetBoardMembersAsync(Guid boardId);
    Task<bool> UserCanAccessBoardAsync(Guid boardId, string userId);
    Task<bool> TaskBelongsToBoardAsync(Guid boardId, Guid taskId);
    Task<Guid> GetWorkspaceIdForBoardAsync(Guid boardId);
    Task<TaskDetailDto?> GetTaskAsync(Guid taskId);
    Task<TaskDetailDto> CreateTaskAsync(CreateTaskDto dto);
    Task UpdateTaskAsync(TaskDetailDto dto);
    Task ForceUpdateTaskAsync(TaskDetailDto dto);
    Task<IReadOnlyList<ReplayEventDto>> GetReplayEventsAsync(Guid boardId, DateTime since);
    Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newSortOrder);

    // Checklists & Comments
    Task<TaskChecklistItemDto> AddChecklistItemAsync(Guid taskId, string text, string actorUserId);
    Task ToggleChecklistItemAsync(Guid itemId, bool isComplete, string actorUserId);
    Task<TaskCommentDto> AddCommentAsync(Guid taskId, string body, string actorUserId, string? clientRequestId = null);

    // Labels
    Task<IReadOnlyList<TaskLabelDto>> GetLabelsForBoardAsync(Guid boardId);
    Task ToggleTaskLabelAsync(Guid taskId, Guid labelId, bool isApplied, string actorUserId);
}
