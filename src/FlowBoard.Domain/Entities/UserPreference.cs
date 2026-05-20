using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class UserPreference : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Theme { get; set; } = "system";
    public string Density { get; set; } = "comfortable";
    public bool SidebarCollapsed { get; set; }
    public Guid? LastWorkspaceId { get; set; }
}
