using System.Runtime.InteropServices;

namespace Ruth.Modules.Engagement.Domain.Streaks;

/// <summary>Computed streak cadence statistics over a dense Span&lt;byte&gt; bitmask.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct StreakComputedMetrics(
    int EngagementCount,
    int CurrentStreakCount,
    DateOnly? CurrentStreakStartDate,
    int LongestStreakCount,
    DateOnly? LongestStreakStartDate,
    DateOnly? LongestStreakEndDate,
    DateOnly? MostRecentEngagementDate
);
