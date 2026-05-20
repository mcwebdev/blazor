using FlowBoard.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.Web.Services;

public sealed class BoardUpdateNotifier(IHubContext<BoardHub> hubContext)
{
    public event Func<BoardRealtimeEvent, Task>? BoardChanged;

    public async Task NotifyBoardChangedAsync(BoardRealtimeEvent change)
    {
        await hubContext.Clients
            .Group(BoardHub.GetBoardGroup(change.BoardId))
            .SendAsync("BoardActivityRecorded", change);

        var handlers = BoardChanged?.GetInvocationList()
            .Cast<Func<BoardRealtimeEvent, Task>>()
            .ToArray();

        if (handlers is null || handlers.Length == 0)
            return;

        foreach (var handler in handlers)
        {
            await handler(change);
        }
    }
}
