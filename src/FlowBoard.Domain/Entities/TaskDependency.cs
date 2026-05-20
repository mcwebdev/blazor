using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

public class TaskDependency : AuditableEntity
{
    public Guid BoardId { get; set; }
    public Guid BlockingTaskId { get; set; }
    public Guid BlockedTaskId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    // Navigation properties
    public TaskItem BlockingTask { get; set; } = null!;
    public TaskItem BlockedTask { get; set; } = null!;
}
