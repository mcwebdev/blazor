using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class TaskComment : AuditableEntity
{
    public Guid TaskItemId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Navigation properties
    public TaskItem TaskItem { get; set; } = null!;
}
