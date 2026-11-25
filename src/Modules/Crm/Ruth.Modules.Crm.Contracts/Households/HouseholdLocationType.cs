namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Type of physical location attached to a household.</summary>
public enum HouseholdLocationType
{
    /// <summary>Primary home street address.</summary>
    Home = 0,

    /// <summary>Dedicated mailing address (e.g. PO Box).</summary>
    Mailing = 1,

    /// <summary>Work address.</summary>
    Work = 2,

    /// <summary>Archived previous address.</summary>
    Previous = 3,
}
