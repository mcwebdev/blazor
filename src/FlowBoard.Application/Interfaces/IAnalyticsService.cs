using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IAnalyticsService
{
    Task<BoardAnalyticsSummaryDto?> GetBoardAnalyticsAsync(Guid boardId, CancellationToken ct = default);

    Task<AnalyticsDrilldownDto?> GetBoardDrilldownAsync(
        Guid boardId,
        AnalyticsMetric metric,
        TaskFilterDto filter,
        CancellationToken ct = default);

    Task<WorkspaceAnalyticsSummaryDto> GetWorkspaceSummaryAsync(Guid workspaceId, CancellationToken ct = default);
}
