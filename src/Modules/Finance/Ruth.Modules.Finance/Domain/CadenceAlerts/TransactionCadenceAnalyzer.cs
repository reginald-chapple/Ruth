using System.Globalization;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>First-principles statistical analyzer detecting constituent lapse/churn risk and unusual amount spikes.</summary>
public static class TransactionCadenceAnalyzer
{
    /// <summary>Evaluates a chronological transaction series against <paramref name="rule" /> as of <paramref name="asOfDate" />.</summary>
    public static CadenceDetectionFinding? Evaluate(
        TransactionCadenceAlertType rule,
        IReadOnlyList<HistoricalTransactionPoint> orderedSeries,
        DateOnly asOfDate,
        bool hasPastDueSubscription = false
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(orderedSeries);

        return !rule.IsActive
            ? null
            : rule.AlertKind switch
            {
                CadenceAlertKind.SubscriptionPastDue => EvaluatePastDue(
                    rule,
                    orderedSeries,
                    hasPastDueSubscription
                ),
                CadenceAlertKind.LapseOrChurnRisk => EvaluateLapseRisk(
                    rule,
                    orderedSeries,
                    asOfDate
                ),
                CadenceAlertKind.UnusualAmountSpike => EvaluateAmountSpike(rule, orderedSeries),
                _ => null,
            };
    }

    private static CadenceDetectionFinding? EvaluatePastDue(
        TransactionCadenceAlertType rule,
        IReadOnlyList<HistoricalTransactionPoint> series,
        bool hasPastDueSubscription
    )
    {
        if (!hasPastDueSubscription)
        {
            return null;
        }

        var meanAmount =
            series.Count > 0
                ? decimal.Round(
                    series.Average(static p => p.NetAmount),
                    2,
                    MidpointRounding.AwayFromZero
                )
                : 0m;

        return meanAmount > 0m && meanAmount < rule.MinimumAmountThreshold
            ? null
            : new CadenceDetectionFinding(
                CadenceAlertKind.SubscriptionPastDue,
                meanAmount,
                BaselineIntervalDays: 30m,
                ObservedValue: 1m,
                "Constituent has a PastDue recurring subscription or contribution schedule."
            );
    }

    private static CadenceDetectionFinding? EvaluateLapseRisk(
        TransactionCadenceAlertType rule,
        IReadOnlyList<HistoricalTransactionPoint> series,
        DateOnly asOfDate
    )
    {
        if (series.Count < rule.MinHistoricalSamples)
        {
            return null;
        }

        var meanAmount = decimal.Round(
            series.Average(static p => p.NetAmount),
            2,
            MidpointRounding.AwayFromZero
        );
        if (meanAmount < rule.MinimumAmountThreshold)
        {
            return null;
        }

        var (meanInterval, stdDevInterval) = ComputeIntervalStats(series);
        if (meanInterval <= 0m)
        {
            return null;
        }

        var daysSinceLast = Math.Max(0, asOfDate.DayNumber - series[^1].Date.DayNumber);
        var ratioCutoff = meanInterval * rule.MultiplierThreshold;
        var stdCutoff = meanInterval + (rule.StandardDeviationSensitivity * stdDevInterval);

        if (daysSinceLast < ratioCutoff || daysSinceLast < stdCutoff)
        {
            return null;
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Elapsed {daysSinceLast} days since last payment vs. {meanInterval:F1}d baseline cadence."
        );
        return new CadenceDetectionFinding(
            CadenceAlertKind.LapseOrChurnRisk,
            meanAmount,
            meanInterval,
            daysSinceLast,
            summary
        );
    }

    private static CadenceDetectionFinding? EvaluateAmountSpike(
        TransactionCadenceAlertType rule,
        IReadOnlyList<HistoricalTransactionPoint> series
    )
    {
        if (series.Count < rule.MinHistoricalSamples + 1)
        {
            return null;
        }

        var latest = series[^1].NetAmount;
        if (latest < rule.MinimumAmountThreshold)
        {
            return null;
        }

        var priorCount = series.Count - 1;
        var priorAmounts = new decimal[priorCount];
        for (var i = 0; i < priorCount; i++)
        {
            priorAmounts[i] = series[i].NetAmount;
        }

        var (meanPrior, stdDevPrior) = ComputeAmountStats(priorAmounts);
        if (meanPrior <= 0m)
        {
            return null;
        }

        var ratioCutoff = meanPrior * rule.MultiplierThreshold;
        var stdCutoff = meanPrior + (rule.StandardDeviationSensitivity * stdDevPrior);
        if (latest < ratioCutoff || latest < stdCutoff)
        {
            return null;
        }

        var (meanInterval, _) = ComputeIntervalStats(series);
        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Observed payment {latest:F2} exceeds historical baseline mean {meanPrior:F2}."
        );
        return new CadenceDetectionFinding(
            CadenceAlertKind.UnusualAmountSpike,
            meanPrior,
            meanInterval,
            latest,
            summary
        );
    }

    private static (decimal Mean, decimal StdDev) ComputeIntervalStats(
        IReadOnlyList<HistoricalTransactionPoint> series
    )
    {
        if (series.Count < 2)
        {
            return (0m, 0m);
        }

        var intervals = new decimal[series.Count - 1];
        for (var i = 1; i < series.Count; i++)
        {
            intervals[i - 1] = Math.Max(1, series[i].Date.DayNumber - series[i - 1].Date.DayNumber);
        }

        return ComputeAmountStats(intervals);
    }

    private static (decimal Mean, decimal StdDev) ComputeAmountStats(ReadOnlySpan<decimal> values)
    {
        var sum = 0m;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        var mean = sum / values.Length;
        var sqDiffSum = 0d;
        for (var i = 0; i < values.Length; i++)
        {
            var diff = (double)(values[i] - mean);
            sqDiffSum += diff * diff;
        }

        var stdDev = (decimal)Math.Sqrt(sqDiffSum / values.Length);
        return (
            decimal.Round(mean, 2, MidpointRounding.AwayFromZero),
            decimal.Round(stdDev, 2, MidpointRounding.AwayFromZero)
        );
    }
}
