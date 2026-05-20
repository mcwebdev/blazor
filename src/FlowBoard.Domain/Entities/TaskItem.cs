using FlowBoard.Domain.Common;
using FlowBoard.Domain.Enums;

namespace FlowBoard.Domain.Entities;

public class TaskItem : AuditableEntity
{
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Open;
    public string? AssigneeUserId { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public DateTime? DueDateUtc { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastModifiedByUserId { get; set; }

    /// <summary>
    /// Optimistic concurrency token for conflict detection.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public Board Board { get; set; } = null!;
    public BoardColumn Column { get; set; } = null!;
    public ICollection<TaskComment> Comments { get; set; } = [];
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = [];
    public ICollection<TaskItemLabel> TaskLabels { get; set; } = [];
}
