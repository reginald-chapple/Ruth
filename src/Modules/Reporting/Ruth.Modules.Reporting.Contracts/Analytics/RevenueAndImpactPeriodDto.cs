namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Monthly revenue and program impact bucket.</summary>
/// <param name="PeriodLabel">Month label (YYYY-MM).</param>
/// <param name="TaxDeductibleGiving">Charitable / tax-deductible donations total.</param>
/// <param name="NonDeductibleDuesAndFees">Membership dues, program fees, commerce, and invoices total.</param>
/// <param name="AttendanceCount">Total attendance count in the period.</param>
/// <param name="ProgramOutcomesCount">Milestones, assistance disbursements, and completions count.</param>
public sealed record RevenueAndImpactPeriodDto(
    string PeriodLabel,
    decimal TaxDeductibleGiving,
    decimal NonDeductibleDuesAndFees,
    int AttendanceCount,
    int ProgramOutcomesCount
);
