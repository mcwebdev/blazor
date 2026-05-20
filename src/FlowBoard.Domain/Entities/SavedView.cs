using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class SavedView : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Guid? BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = "{}";
    public bool IsDefault { get; set; }
}
