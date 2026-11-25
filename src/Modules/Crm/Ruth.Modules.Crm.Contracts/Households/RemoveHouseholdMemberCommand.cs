using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to remove a person from a household.</summary>
public sealed record RemoveHouseholdMemberCommand(Guid HouseholdId, Guid PersonId)
    : ICommand<HouseholdResponse>;
