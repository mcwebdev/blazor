using System.Security.Claims;
using FlowBoard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Hubs;

[Authorize]
public sealed class BoardHub(IPresenceService presenceService) : Hub<IBoardClient>
{
    public async Task JoinBoard(Guid boardId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        if (userId is null)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetBoardGroup(boardId));
        await presenceService.JoinBoardAsync(boardId, Context.ConnectionId, userId, userName);

        var activeUsers = await presenceService.GetActiveUsersAsync(boardId);
        await Clients.Group(GetBoardGroup(boardId)).UserJoinedBoard(activeUsers);
    }

    public async Task LeaveBoard(Guid boardId)
    {
        await presenceService.LeaveBoardAsync(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroup(boardId));

        var activeUsers = await presenceService.GetActiveUsersAsync(boardId);
        await Clients.Group(GetBoardGroup(boardId)).UserLeftBoard(activeUsers);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var boardId = await presenceService.LeaveBoardAsync(Context.ConnectionId);
        
        if (boardId.HasValue)
        {
            var activeUsers = await presenceService.GetActiveUsersAsync(boardId.Value);
            await Clients.Group(GetBoardGroup(boardId.Value)).UserLeftBoard(activeUsers);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartEditing(Guid boardId, Guid taskId, string fieldName)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.Group(GetBoardGroup(boardId)).UserStartedEditing(userName, taskId, fieldName);
    }

    public async Task StopEditing(Guid boardId, Guid taskId, string fieldName)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.Group(GetBoardGroup(boardId)).UserStoppedEditing(userName, taskId, fieldName);
    }

    public static string GetBoardGroup(Guid boardId) => $"board:{boardId}";
}

