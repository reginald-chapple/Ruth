namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Role of a person within a household (family).</summary>
public enum HouseholdRole
{
    /// <summary>Adult member (head of household / parent / guardian).</summary>
    Adult = 0,

    /// <summary>Child / dependent member.</summary>
    Child = 1,
}
