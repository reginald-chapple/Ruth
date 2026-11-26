using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Command to generate a tax receipt or account statement for an individual, household, or organization sponsor.</summary>
public sealed record GenerateFinancialStatementCommand(
    Guid TemplateId,
    FinancialStatementRecipientScope RecipientScope,
    Guid? TargetPersonId,
    Guid? TargetHouseholdId,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    bool PersistArchiveToStorage = true
) : ICommand<RenderedFinancialStatementDocumentResponse>;
