using FlowBoard.Application.DTOs;
using FlowBoard.Application.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Services;

public class FeatureFlagService : IFeatureFlagService
{
    // The canonical set of flags the spec calls out (§4.16, §5.4). The
    // service backfills missing rows on first read so the UI always shows
    // the full toggle set even on a brand-new workspace.
    private static readonly (string Key, string DisplayName, string Description, bool DefaultEnabled)[] CanonicalFlags =
    [
        ("analytics", "Analytics", "Show the /analytics surface and the board analytics drill-down.", true),
        ("notifications", "Notifications", "Show the in-app notification bell and queue assignment notifications.", true),
        ("replay", "Board Replay", "Allow users to open the board replay timeline panel.", true),
        ("experimental-ui", "Experimental UI", "Enable in-progress UI experiments such as draft auto-save.", false)
    ];

    private readonly FlowBoardDbContext _db;

    public FeatureFlagService(FlowBoardDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FeatureFlagRowDto>> GetFlagsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanonicalFlagsAsync(workspaceId, cancellationToken);

        var rows = await _db.FeatureFlags
            .AsNoTracking()
            .Where(f => f.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        // Project with the descriptive metadata from the canonical list so
        // the page can show a human-readable label/description even for
        // flags that were never toggled.
        var keyed = rows.ToDictionary(r => r.Key, r => r);

        return CanonicalFlags
            .Select(canonical =>
            {
                var row = keyed[canonical.Key];
                return new FeatureFlagRowDto(
                    row.Id,
                    row.WorkspaceId,
                    row.Key,
                    canonical.DisplayName,
                    canonical.Description,
                    row.IsEnabled,
                    row.UpdatedAtUtc ?? row.CreatedAtUtc,
                    row.UpdatedByUserId);
            })
            .ToList();
    }

    public async Task SetFlagAsync(
        Guid workspaceId,
        string key,
        bool isEnabled,
        string? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanonicalFlagsAsync(workspaceId, cancellationToken);

        var existing = await _db.FeatureFlags
            .FirstOrDefaultAsync(f => f.WorkspaceId == workspaceId && f.Key == key, cancellationToken);

        if (existing is null)
            return;

        if (existing.IsEnabled == isEnabled)
            return;

        existing.IsEnabled = isEnabled;
        existing.UpdatedByUserId = updatedByUserId;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsEnabledAsync(
        Guid workspaceId,
        string key,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.FeatureFlags
            .AsNoTracking()
            .Where(f => f.WorkspaceId == workspaceId && f.Key == key)
            .Select(f => (bool?)f.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);

        if (row.HasValue)
            return row.Value;

        // Unknown key: fall back to the canonical default if we know the
        // key, otherwise default closed.
        return CanonicalFlags.FirstOrDefault(f => f.Key == key).DefaultEnabled;
    }

    private async Task EnsureCanonicalFlagsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var existingKeys = await _db.FeatureFlags
            .Where(f => f.WorkspaceId == workspaceId)
            .Select(f => f.Key)
            .ToListAsync(cancellationToken);

        var missing = CanonicalFlags
            .Where(f => !existingKeys.Contains(f.Key))
            .ToArray();

        if (missing.Length == 0)
            return;

        foreach (var canonical in missing)
        {
            _db.FeatureFlags.Add(new FeatureFlag
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Key = canonical.Key,
                IsEnabled = canonical.DefaultEnabled,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
