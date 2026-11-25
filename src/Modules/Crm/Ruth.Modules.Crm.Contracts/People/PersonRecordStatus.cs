namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Lifecycle status of a CRM person record.</summary>
public enum PersonRecordStatus
{
    /// <summary>Record is active in the directory.</summary>
    Active = 0,

    /// <summary>Record is inactive (archived, deceased, or merged).</summary>
    Inactive = 1,

    /// <summary>Record is awaiting review or confirmation.</summary>
    Pending = 2,
}
