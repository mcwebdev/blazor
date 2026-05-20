using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class Board : AuditableEntity
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArchived { get; set; }

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public ICollection<BoardColumn> Columns { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
