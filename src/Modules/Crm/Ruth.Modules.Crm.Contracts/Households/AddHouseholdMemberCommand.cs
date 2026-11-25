using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to add or update a member in a household (supports blended multi-household memberships).</summary>
public sealed record AddHouseholdMemberCommand(
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role = HouseholdRole.Adult,
    int HouseholdOrder = 0,
    bool IsCombinedContributionMember = true,
    bool? IsPrimaryContact = null
) : ICommand<HouseholdResponse>;
