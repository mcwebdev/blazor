namespace FlowBoard.Web.Utilities;

public static class FormatHelper
{
    public static string GetInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        var parts = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => part[0].ToString().ToUpperInvariant())
            .ToArray();

        return parts.Length == 0 ? "?" : string.Concat(parts);
    }

    public static string FormatRelativeTime(DateTime createdAtUtc)
    {
        var timestamp = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        var elapsed = DateTime.UtcNow - timestamp;

        if (elapsed < TimeSpan.FromMinutes(1))
            return "just now";

        if (elapsed < TimeSpan.FromHours(1))
            return $"{Math.Max(1, (int)elapsed.TotalMinutes)}m ago";

        if (elapsed < TimeSpan.FromDays(1))
            return $"{Math.Max(1, (int)elapsed.TotalHours)}h ago";

        return $"{Math.Max(1, (int)elapsed.TotalDays)}d ago";
    }

    public static string FormatTaskDate(DateTime dueDateUtc)
    {
        return dueDateUtc.ToString("MMM d");
    }
}
