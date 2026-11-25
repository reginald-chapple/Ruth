using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to update household metadata (name, campus, active/giving flags).</summary>
public sealed record UpdateHouseholdCommand(
    Guid HouseholdId,
    string Name,
    Guid? CampusId = null,
    bool IsActive = true,
    bool CombineGivingByDefault = true
) : ICommand<HouseholdResponse>;
