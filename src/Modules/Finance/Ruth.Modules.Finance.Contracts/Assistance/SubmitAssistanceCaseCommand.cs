using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to submit an assistance, scholarship, fee-waiver, or micro-grant intake case.</summary>
public sealed record SubmitAssistanceCaseCommand(
    Guid ProgramId,
    Guid ApplicantPersonId,
    decimal RequestedAmount,
    string Summary,
    string? RequestDetails = null,
    Guid? HouseholdId = null,
    Guid? BranchId = null,
    Guid? AssignedCaseworkerPersonId = null,
    IReadOnlyDictionary<string, string?>? Attributes = null
) : ICommand<AssistanceCaseResponse>;
