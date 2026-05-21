using FlowBoard.Application.DTOs;
using FlowBoard.Domain.Enums;

namespace FlowBoard.Application.Interfaces;

public interface IAuditQueryService
{
    // Returns the caller's role on the workspace, or null if they are not
    // a member. Admin pages use this to gate access.
    Task<WorkspaceRole?> GetUserRoleAsync(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken = default);

    // Audit log (paged, filterable). QuickGrid calls this from its
    // ItemsProvider on every page/sort/filter change.
    Task<AuditLogPageDto> GetAuditLogPageAsync(
        Guid workspaceId,
        AuditLogFilterDto filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    // Distinct list of actor user ids (paired with display names) used to
    // populate the audit filter dropdown.
    Task<IReadOnlyList<WorkspaceMemberRowDto>> GetWorkspaceMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    // Boards/columns whose CURRENT task count meets or exceeds the
    // configured WipLimit. Empty list when no breaches exist.
    Task<IReadOnlyList<WipBreachRowDto>> GetWipBreachesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    // FailedCommand rows for the audit grid. Newest first.
    Task<IReadOnlyList<FailedCommandRowDto>> GetFailedCommandsAsync(
        Guid workspaceId,
        int take = 200,
        CancellationToken cancellationToken = default);

    // Persists a FailedCommand row (called from the board reconnect
    // drain when an action exhausts retries).
    Task RecordFailedCommandAsync(
        Guid workspaceId,
        string userId,
        string commandType,
        string payloadJson,
        string errorSummary,
        Guid? correlationId,
        CancellationToken cancellationToken = default);
}
