using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class TaskLabel : AuditableEntity
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#276ef1";

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public ICollection<TaskItemLabel> TaskItems { get; set; } = [];
}
