using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto?> GetBoardAsync(Guid boardId);
    Task<IReadOnlyList<BoardDto>> GetBoardsForWorkspaceAsync(Guid workspaceId);
}
