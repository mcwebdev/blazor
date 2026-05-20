using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class BoardColumn : AuditableEntity
{
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? WipLimit { get; set; }

    // Navigation properties
    public Board Board { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
