using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Hubs;

[Authorize]
public sealed class BoardHub : Hub
{
    public Task JoinBoard(Guid boardId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GetBoardGroup(boardId));
    }

    public Task LeaveBoard(Guid boardId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroup(boardId));
    }

    public static string GetBoardGroup(Guid boardId) => $"board:{boardId}";
}

