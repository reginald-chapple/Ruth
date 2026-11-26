namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Revenue &amp; Impact Overview dashboard snapshot covering donations, dues/MRR, retention, attendance, and program outcomes.</summary>
/// <param name="StartDateUtc">Inclusive window start in UTC.</param>
/// <param name="EndDateUtc">Inclusive window end in UTC.</param>
/// <param name="BranchId">Optional Branch filter.</param>
/// <param name="TotalTaxDeductibleGiving">Sum of tax-deductible charitable contributions.</param>
/// <param name="TotalNonDeductibleRevenue">Sum of non-deductible dues, fees, invoices, and commerce.</param>
/// <param name="MonthlyRecurringRevenue">Active normalized monthly recurring revenue (MRR).</param>
/// <param name="ActiveRecurringSchedules">Count of active recurring schedules.</param>
/// <param name="RetentionRatePercent">Repeat contributor/member retention rate percentage (0-100).</param>
/// <param name="TotalAttendance">Total recorded attendance check-ins and occurrences.</param>
/// <param name="AssistanceDisbursementsTotal">Total community assistance/grant disbursements.</param>
/// <param name="ProgramCompletionsCount">Total journeys/milestones/LMS course completions.</param>
/// <param name="Periods">Monthly breakdown series.</param>
public sealed record RevenueAndImpactOverviewResponse(
    DateTimeOffset StartDateUtc,
    DateTimeOffset EndDateUtc,
    Guid? BranchId,
    decimal TotalTaxDeductibleGiving,
    decimal TotalNonDeductibleRevenue,
    decimal MonthlyRecurringRevenue,
    int ActiveRecurringSchedules,
    decimal RetentionRatePercent,
    int TotalAttendance,
    decimal AssistanceDisbursementsTotal,
    int ProgramCompletionsCount,
    IReadOnlyList<RevenueAndImpactPeriodDto> Periods
);
