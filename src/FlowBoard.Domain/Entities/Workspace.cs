using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class Workspace : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    // Navigation properties
    public ICollection<WorkspaceMember> Members { get; set; } = [];
    public ICollection<Board> Boards { get; set; } = [];
    public ICollection<TaskLabel> Labels { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
