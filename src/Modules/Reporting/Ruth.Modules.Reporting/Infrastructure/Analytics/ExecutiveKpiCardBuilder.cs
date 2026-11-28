using System.Globalization;
using Ruth.Modules.Reporting.Contracts.Analytics;

namespace Ruth.Modules.Reporting.Infrastructure.Analytics;

internal static class ExecutiveKpiCardBuilder
{
    public static IReadOnlyList<ExecutiveKpiCardDto> BuildKpiCards(
        int windowDays,
        decimal totalPeople,
        decimal activeGroups,
        int currentAttendance,
        int priorAttendance,
        RevenueAndImpactOverviewResponse overview,
        RevenueAndImpactOverviewResponse priorOverview
    )
    {
        var winLabel = windowDays.ToString(CultureInfo.InvariantCulture);
        List<ExecutiveKpiCardDto> cards =
        [
            CreateKpi(
                "constituents.total",
                "Active Constituents",
                totalPeople,
                "People",
                delta: null,
                [totalPeople]
            ),
            CreateKpi(
                "groups.active",
                "Active Groups & Teams",
                activeGroups,
                "Groups",
                delta: null,
                [activeGroups]
            ),
            CreateKpi(
                "attendance.window",
                $"Attendance ({winLabel}d)",
                currentAttendance,
                "Check-Ins",
                ComputeDelta(currentAttendance, priorAttendance),
                [priorAttendance, currentAttendance]
            ),
        ];
        cards.AddRange(BuildFinanceAndImpactKpis(winLabel, overview, priorOverview));
        return cards;
    }

    private static IReadOnlyList<ExecutiveKpiCardDto> BuildFinanceAndImpactKpis(
        string winLabel,
        RevenueAndImpactOverviewResponse overview,
        RevenueAndImpactOverviewResponse priorOverview
    )
    {
        return
        [
            CreateCurrencyKpi(
                "finance.deductible",
                $"Tax-Deductible Giving ({winLabel}d)",
                overview.TotalTaxDeductibleGiving,
                "USD",
                ComputeDelta(
                    overview.TotalTaxDeductibleGiving,
                    priorOverview.TotalTaxDeductibleGiving
                ),
                [priorOverview.TotalTaxDeductibleGiving, overview.TotalTaxDeductibleGiving]
            ),
            CreateCurrencyKpi(
                "finance.dues-mrr",
                "Monthly Recurring Revenue (MRR)",
                overview.MonthlyRecurringRevenue,
                "USD/mo",
                delta: null,
                [overview.MonthlyRecurringRevenue]
            ),
            new ExecutiveKpiCardDto(
                "impact.retention",
                "Contributor & Member Retention",
                string.Create(CultureInfo.InvariantCulture, $"{overview.RetentionRatePercent:N1}%"),
                overview.RetentionRatePercent,
                "%",
                DeltaPercent: null,
                [overview.RetentionRatePercent]
            ),
            CreateKpi(
                "impact.outcomes",
                $"Program Outcomes ({winLabel}d)",
                overview.ProgramCompletionsCount,
                "Completions",
                ComputeDelta(
                    overview.ProgramCompletionsCount,
                    priorOverview.ProgramCompletionsCount
                ),
                [priorOverview.ProgramCompletionsCount, overview.ProgramCompletionsCount]
            ),
        ];
    }

    private static ExecutiveKpiCardDto CreateKpi(
        string key,
        string title,
        decimal value,
        string unit,
        decimal? delta,
        IReadOnlyList<decimal> spark
    )
    {
        return new ExecutiveKpiCardDto(
            key,
            title,
            value.ToString("N0", CultureInfo.InvariantCulture),
            value,
            unit,
            delta,
            spark
        );
    }

    private static ExecutiveKpiCardDto CreateCurrencyKpi(
        string key,
        string title,
        decimal value,
        string unit,
        decimal? delta,
        IReadOnlyList<decimal> spark
    )
    {
        return new ExecutiveKpiCardDto(
            key,
            title,
            string.Create(CultureInfo.InvariantCulture, $"${value:N2}"),
            value,
            unit,
            delta,
            spark
        );
    }

    private static decimal? ComputeDelta(decimal current, decimal previous)
    {
        return previous == 0m
            ? null
            : Math.Round(
                (current - previous) / Math.Abs(previous) * 100m,
                2,
                MidpointRounding.AwayFromZero
            );
    }
}
