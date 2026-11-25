namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Adult vs. child demographic classification.</summary>
public enum AgeClassification
{
    /// <summary>Classification cannot yet be determined.</summary>
    Unknown = 0,

    /// <summary>Adult (18+ or Adult household role, not locked as child).</summary>
    Adult = 1,

    /// <summary>Child (under 18, Child role, or locked as child).</summary>
    Child = 2,
}
