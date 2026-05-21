namespace FlowBoard.Application.DTOs;

public record WorkspaceAnalyticsSummaryDto(
    int TotalBoards,
    int TotalTasks,
    int TotalMembers,
    int OverdueTasks,
    int CompletedThisWeek,
    IReadOnlyList<WorkspaceBoardSummaryDto> Boards
);

public record WorkspaceBoardSummaryDto(
    Guid BoardId,
    string Name,
    int ColumnCount,
    int TaskCount,
    int OverdueTasks,
    int CompletedTasks
);
