using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to create a new household or member unit with optional initial members and branch.</summary>
public sealed record CreateHouseholdCommand(
    string Name,
    Guid? BranchId = null,
    bool CombineContributionsByDefault = true,
    IReadOnlyList<HouseholdMemberInputDto>? Members = null
) : ICommand<HouseholdResponse>;
