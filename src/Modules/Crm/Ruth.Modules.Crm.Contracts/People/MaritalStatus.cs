namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Marital status of a person.</summary>
public enum MaritalStatus
{
    /// <summary>Marital status not specified.</summary>
    Unknown = 0,

    /// <summary>Unmarried / single.</summary>
    Unmarried = 1,

    /// <summary>Married.</summary>
    Married = 2,

    /// <summary>Divorced.</summary>
    Divorced = 3,

    /// <summary>Widowed.</summary>
    Widowed = 4,
}
