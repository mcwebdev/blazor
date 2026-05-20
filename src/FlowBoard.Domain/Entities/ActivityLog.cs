using FlowBoard.Domain.Common;
using FlowBoard.Domain.Enums;

namespace FlowBoard.Domain.Entities;

public class ActivityLog : AuditableEntity
{
    public Guid WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public Guid? TaskItemId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public ActivityEventType EventType { get; set; }
    public ActivityEventCategory EventCategory { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? MetadataJson { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
}
