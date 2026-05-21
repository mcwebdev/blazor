using System.Collections.Concurrent;
using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;

namespace FlowBoard.Infrastructure.Services;

public sealed class PresenceService : IPresenceService
{
    // Maps ConnectionId -> (BoardId, UserId, UserName)
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    
    // Maps BoardId -> HashSet of ConnectionIds
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _boardConnections = new();

    public Task JoinBoardAsync(Guid boardId, string connectionId, string userId, string userName)
    {
        var info = new ConnectionInfo(boardId, userId, userName);
        
        if (_connections.TryAdd(connectionId, info))
        {
            _boardConnections.AddOrUpdate(
                boardId,
                _ => new HashSet<string> { connectionId },
                (_, set) =>
                {
                    lock (set) { set.Add(connectionId); }
                    return set;
                });
        }
        
        return Task.CompletedTask;
    }

    public Task<Guid?> LeaveBoardAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            if (_boardConnections.TryGetValue(info.BoardId, out var set))
            {
                lock (set)
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                    {
                        _boardConnections.TryRemove(info.BoardId, out _);
                    }
                }
            }
            return Task.FromResult<Guid?>(info.BoardId);
        }

        return Task.FromResult<Guid?>(null);
    }

    public Task<IReadOnlyList<BoardPresenceDto>> GetActiveUsersAsync(Guid boardId)
    {
        if (!_boardConnections.TryGetValue(boardId, out var set))
        {
            return Task.FromResult<IReadOnlyList<BoardPresenceDto>>([]);
        }

        List<string> activeConnectionIds;
        lock (set)
        {
            activeConnectionIds = set.ToList();
        }

        var uniqueUsers = new Dictionary<string, BoardPresenceDto>();
        
        foreach (var connId in activeConnectionIds)
        {
            if (_connections.TryGetValue(connId, out var info))
            {
                // De-duplicate users opening multiple tabs
                if (!uniqueUsers.ContainsKey(info.UserId))
                {
                    uniqueUsers[info.UserId] = new BoardPresenceDto(info.UserId, info.UserName);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<BoardPresenceDto>>(uniqueUsers.Values.ToList());
    }

    private sealed record ConnectionInfo(Guid BoardId, string UserId, string UserName);
}
