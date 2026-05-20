using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class ChecklistItem : AuditableEntity
{
    public Guid TaskItemId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public TaskItem TaskItem { get; set; } = null!;
}
