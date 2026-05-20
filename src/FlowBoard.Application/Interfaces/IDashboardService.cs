using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardAsync(string userId, Guid workspaceId);
}
