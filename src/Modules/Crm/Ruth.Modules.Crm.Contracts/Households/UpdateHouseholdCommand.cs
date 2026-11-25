using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to update household metadata (name, branch, active/combined-contribution flags).</summary>
public sealed record UpdateHouseholdCommand(
    Guid HouseholdId,
    string Name,
    Guid? BranchId = null,
    bool IsActive = true,
    bool CombineContributionsByDefault = true
) : ICommand<HouseholdResponse>;
