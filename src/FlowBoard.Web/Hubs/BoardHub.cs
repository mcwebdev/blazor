using System.Security.Claims;
using FlowBoard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Hubs;

[Authorize]
public sealed class BoardHub(
    IPresenceService presenceService,
    IBoardService boardService,
    ILogger<BoardHub> logger) : Hub<IBoardClient>
{
    public async Task JoinBoard(Guid boardId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        if (userId is null)
            return;

        if (!await boardService.UserCanAccessBoardAsync(boardId, userId))
        {
            logger.LogWarning("User {UserId} attempted to join unauthorized board {BoardId}.", userId, boardId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetBoardGroup(boardId));
        await presenceService.JoinBoardAsync(boardId, Context.ConnectionId, userId, userName);

        var activeUsers = await presenceService.GetActiveUsersAsync(boardId);
        await Clients.Group(GetBoardGroup(boardId)).UserJoinedBoard(activeUsers);
    }

    public async Task LeaveBoard(Guid boardId)
    {
        var activeBoardId = await presenceService.LeaveBoardAsync(Context.ConnectionId);

        if (activeBoardId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroup(activeBoardId.Value));
            var activeUsers = await presenceService.GetActiveUsersAsync(activeBoardId.Value);
            await Clients.Group(GetBoardGroup(activeBoardId.Value)).UserLeftBoard(activeUsers);
        }
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
        if (!await CanBroadcastEditingStateAsync(boardId, taskId))
            return;

        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(GetBoardGroup(boardId)).UserStartedEditing(userName, taskId, fieldName);
    }

    public async Task StopEditing(Guid boardId, Guid taskId, string fieldName)
    {
        if (!await CanBroadcastEditingStateAsync(boardId, taskId))
            return;

        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(GetBoardGroup(boardId)).UserStoppedEditing(userName, taskId, fieldName);
    }

    private async Task<bool> CanBroadcastEditingStateAsync(Guid boardId, Guid taskId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return false;

        var canAccessBoard = await boardService.UserCanAccessBoardAsync(boardId, userId);
        if (!canAccessBoard)
        {
            logger.LogWarning("User {UserId} attempted to broadcast editing state to unauthorized board {BoardId}.", userId, boardId);
            return false;
        }

        var taskBelongsToBoard = await boardService.TaskBelongsToBoardAsync(boardId, taskId);
        if (!taskBelongsToBoard)
        {
            logger.LogWarning("User {UserId} attempted to broadcast editing state for task {TaskId} outside board {BoardId}.", userId, taskId, boardId);
            return false;
        }

        return true;
    }

    public static string GetBoardGroup(Guid boardId) => $"board:{boardId}";
}
