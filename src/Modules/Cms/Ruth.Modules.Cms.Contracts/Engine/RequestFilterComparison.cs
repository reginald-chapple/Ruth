namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Comparison operator applied to a RequestFilterRule.</summary>
public enum RequestFilterComparison
{
    /// <summary>Case-insensitive exact string match.</summary>
    Equals = 0,

    /// <summary>Case-insensitive non-equal check.</summary>
    NotEquals = 1,

    /// <summary>Case-insensitive substring match.</summary>
    Contains = 2,

    /// <summary>Case-insensitive prefix match.</summary>
    StartsWith = 3,

    /// <summary>Key exists with a non-empty value.</summary>
    Exists = 4,

    /// <summary>Key is missing or empty.</summary>
    NotExists = 5,

    /// <summary>Value falls inside the specified range or CIDR block.</summary>
    InRange = 6,
}
