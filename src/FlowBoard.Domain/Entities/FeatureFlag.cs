using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class FeatureFlag : AuditableEntity
{
    public Guid WorkspaceId { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? UpdatedByUserId { get; set; }
}
