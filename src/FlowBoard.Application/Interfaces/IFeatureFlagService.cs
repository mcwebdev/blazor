using FlowBoard.Application.DTOs;

namespace FlowBoard.Application.Interfaces;

public interface IFeatureFlagService
{
    // Returns the canonical list of workspace flags. Missing rows are
    // backfilled with defaults on first read so the UI never has to render
    // an empty table.
    Task<IReadOnlyList<FeatureFlagRowDto>> GetFlagsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    // Toggles a flag. Tracks updater id and timestamp.
    Task SetFlagAsync(
        Guid workspaceId,
        string key,
        bool isEnabled,
        string? updatedByUserId,
        CancellationToken cancellationToken = default);

    // Quick check for hot paths. Returns the configured value or the
    // shipped default if no row exists.
    Task<bool> IsEnabledAsync(
        Guid workspaceId,
        string key,
        CancellationToken cancellationToken = default);
}
