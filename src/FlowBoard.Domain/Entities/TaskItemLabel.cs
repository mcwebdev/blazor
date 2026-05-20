namespace FlowBoard.Domain.Entities;

public class TaskItemLabel
{
    public Guid TaskItemId { get; set; }
    public Guid TaskLabelId { get; set; }

    // Navigation properties
    public TaskItem TaskItem { get; set; } = null!;
    public TaskLabel TaskLabel { get; set; } = null!;
}
