namespace FlowBoard.Domain.Enums;

public enum ActivityEventType
{
    TaskCreated,
    TaskUpdated,
    TaskMoved,
    TaskDeleted,
    TaskAssigned,
    TaskPriorityChanged,
    TaskDueDateChanged,
    TaskCompleted,
    CommentAdded,
    CommentUpdated,
    BoardCreated,
    BoardArchived,
    ColumnCreated,
    ColumnReordered,
    MemberAdded,
    MemberRemoved,
    MemberRoleChanged
}
