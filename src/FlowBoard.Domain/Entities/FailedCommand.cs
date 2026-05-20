using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class FailedCommand : AuditableEntity
{
    public Guid WorkspaceId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ErrorSummary { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
}
