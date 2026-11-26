namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Specification for creating or updating an assistance, scholarship, fee-waiver, or micro-grant program.</summary>
public sealed record AssistanceProgramSpec(
    string Name,
    string Code,
    AssistanceProgramCategory Category,
    Guid FundingAccountId,
    decimal TotalBudgetAmount,
    decimal MaxAwardPerCase,
    string CurrencyCode = "USD",
    string? Description = null,
    bool RequiresDocumentation = false,
    Guid? BranchId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    bool IsActive = true
);
