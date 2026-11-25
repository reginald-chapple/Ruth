using Ruth.Modules.Crm.Contracts.Relationships;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Pure domain calculator implementing Rock RMS PeerNetwork growth (+10%/mo) and decay (-20%/mo) scoring.</summary>
public static class PeerNetworkScoreCalculator
{
    /// <summary>Computes the updated relationship score and movement trend.</summary>
    public static (decimal Score, RelationshipTrend Trend) Calculate(
        RelationshipStrength strength,
        DateOnly startDate,
        DateOnly? endDate,
        DateOnly today,
        decimal rolePairMultiplier = 1.0m,
        bool isGrowthEnabled = true,
        decimal? previousScore = null
    )
    {
        if (strength == RelationshipStrength.None || rolePairMultiplier <= 0m)
        {
            return (0m, ResolveTrend(0m, previousScore));
        }

        var baseScore = (decimal)(int)strength;
        var growthMultiplier = endDate.HasValue ? -0.2m : 0.1m;
        var referenceDate = endDate ?? startDate;
        var months = Math.Max(
            0,
            ((today.Year - referenceDate.Year) * 12) + today.Month - referenceDate.Month
        );

        var raw =
            isGrowthEnabled || growthMultiplier < 0m
                ? (baseScore + (months * growthMultiplier)) * rolePairMultiplier
                : baseScore * rolePairMultiplier;

        var clamped = Math.Max(0m, Math.Round(raw, 1, MidpointRounding.AwayFromZero));
        return (clamped, ResolveTrend(clamped, previousScore));
    }

    private static RelationshipTrend ResolveTrend(decimal currentScore, decimal? previousScore)
    {
        return !previousScore.HasValue || currentScore == previousScore.Value
                ? RelationshipTrend.Level
            : currentScore > previousScore.Value ? RelationshipTrend.Up
            : RelationshipTrend.Down;
    }
}
