namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Represents a person's membership inside a household.</summary>
public sealed record HouseholdMemberResponse(
    Guid Id,
    Guid PersonId,
    string FullName,
    HouseholdRole Role,
    int HouseholdOrder,
    bool IsCombinedGivingMember
);
