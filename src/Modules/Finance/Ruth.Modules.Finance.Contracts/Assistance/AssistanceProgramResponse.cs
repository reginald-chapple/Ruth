namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Read projection for a configured assistance or grant program.</summary>
public sealed record AssistanceProgramResponse(
    Guid Id,
    string Name,
    string Code,
    AssistanceProgramCategory Category,
    Guid FundingAccountId,
    string CurrencyCode,
    decimal TotalBudgetAmount,
    decimal DisbursedAmount,
    decimal RemainingBudgetAmount,
    decimal MaxAwardPerCase,
    string? Description,
    bool RequiresDocumentation,
    Guid? BranchId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
