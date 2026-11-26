using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Command to execute a batch statement or annual tax receipt run across qualifying constituents, households, and organizations.</summary>
public sealed record RunFinancialStatementBatchCommand(
    Guid TemplateId,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    bool ConsolidateHouseholds = true,
    bool IncludeOrganizations = true,
    bool PersistArchiveToStorage = true
) : ICommand<FinancialStatementBatchRunResponse>;
