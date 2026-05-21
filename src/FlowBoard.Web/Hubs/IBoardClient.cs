using FlowBoard.Application.DTOs;

namespace FlowBoard.Web.Hubs;

public interface IBoardClient
{
    Task UserJoinedBoard(IReadOnlyList<BoardPresenceDto> users);
    Task UserLeftBoard(IReadOnlyList<BoardPresenceDto> users);
    Task BoardActivityRecorded(BoardRealtimeEvent evt);
    Task UserStartedEditing(string userName, Guid taskId, string fieldName);
    Task UserStoppedEditing(string userName, Guid taskId, string fieldName);
}
