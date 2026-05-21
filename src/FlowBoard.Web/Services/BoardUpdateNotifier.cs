using FlowBoard.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Services;

public sealed class BoardUpdateNotifier(IHubContext<BoardHub, IBoardClient> hubContext)
{
    public async Task NotifyBoardChangedAsync(BoardRealtimeEvent change)
    {
        await hubContext.Clients
            .Group(BoardHub.GetBoardGroup(change.BoardId))
            .BoardActivityRecorded(change);
    }
}
