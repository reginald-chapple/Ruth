using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to create a new household (family) with optional initial members and campus.</summary>
public sealed record CreateHouseholdCommand(
    string Name,
    Guid? CampusId = null,
    bool CombineGivingByDefault = true,
    IReadOnlyList<HouseholdMemberInputDto>? Members = null
) : ICommand<HouseholdResponse>;
