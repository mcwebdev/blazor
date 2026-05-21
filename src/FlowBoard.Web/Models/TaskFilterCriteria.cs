using FlowBoard.Domain.Enums;

namespace FlowBoard.Web.Models;

public class TaskFilterCriteria
{
    public string SearchText { get; set; } = string.Empty;
    public TaskPriority? Priority { get; set; }
    public string? AssigneeUserId { get; set; }
}
