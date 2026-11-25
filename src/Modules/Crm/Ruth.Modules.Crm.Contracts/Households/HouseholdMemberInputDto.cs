namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Input specification for adding a person to a household.</summary>
public sealed record HouseholdMemberInputDto(
    Guid PersonId,
    HouseholdRole Role = HouseholdRole.Adult,
    int HouseholdOrder = 0,
    bool IsCombinedGivingMember = true
);
