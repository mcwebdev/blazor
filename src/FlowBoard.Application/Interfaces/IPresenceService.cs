using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IPresenceService
{
    Task JoinBoardAsync(Guid boardId, string connectionId, string userId, string userName);
    Task<Guid?> LeaveBoardAsync(string connectionId);
    Task<IReadOnlyList<BoardPresenceDto>> GetActiveUsersAsync(Guid boardId);
}
