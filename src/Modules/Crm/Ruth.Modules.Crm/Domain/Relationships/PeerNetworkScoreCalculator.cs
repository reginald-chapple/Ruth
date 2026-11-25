using Ruth.Modules.Crm.Contracts.Relationships;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>
/// Pure domain calculator computing peer-network relationship scores with bounded tenure reinforcement
/// (+5%/month capped at +50%) and 6-month exponential half-life decay after an edge ends.
/// </summary>
public static class PeerNetworkScoreCalculator
{
    private const decimal MonthlyReinforcementRate = 0.05m;
    private const decimal MaxReinforcementBonus = 0.50m;
    private const double DecayHalfLifeMonths = 6.0d;
    private const decimal ZeroFloorThreshold = 0.05m;

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

        var baseWeight = (int)strength * rolePairMultiplier;
        var raw = endDate.HasValue
            ? ComputeDecayedScore(baseWeight, endDate.Value, today)
            : ComputeActiveScore(baseWeight, startDate, today, isGrowthEnabled);

        var clamped =
            raw < ZeroFloorThreshold ? 0m : Math.Round(raw, 2, MidpointRounding.AwayFromZero);
        return (clamped, ResolveTrend(clamped, previousScore));
    }

    private static decimal ComputeActiveScore(
        decimal baseWeight,
        DateOnly startDate,
        DateOnly today,
        bool isGrowthEnabled
    )
    {
        if (!isGrowthEnabled)
        {
            return baseWeight;
        }

        var activeMonths = CountWholeMonths(startDate, today);
        var bonusFactor = Math.Min(MaxReinforcementBonus, activeMonths * MonthlyReinforcementRate);
        return baseWeight * (1m + bonusFactor);
    }

    private static decimal ComputeDecayedScore(decimal baseWeight, DateOnly endDate, DateOnly today)
    {
        var inactiveMonths = CountWholeMonths(endDate, today);
        if (inactiveMonths <= 0)
        {
            return baseWeight;
        }

        var decayFactor = (decimal)Math.Pow(0.5d, inactiveMonths / DecayHalfLifeMonths);
        return baseWeight * decayFactor;
    }

    private static int CountWholeMonths(DateOnly from, DateOnly to)
    {
        return Math.Max(0, ((to.Year - from.Year) * 12) + to.Month - from.Month);
    }

    private static RelationshipTrend ResolveTrend(decimal currentScore, decimal? previousScore)
    {
        return !previousScore.HasValue || currentScore == previousScore.Value
                ? RelationshipTrend.Level
            : currentScore > previousScore.Value ? RelationshipTrend.Up
            : RelationshipTrend.Down;
    }
}
