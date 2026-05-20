namespace FlowBoard.Web.Hubs;

public sealed record BoardRealtimeEvent(
    Guid BoardId,
    string EventType,
    string Summary,
    DateTime CreatedAtUtc);
