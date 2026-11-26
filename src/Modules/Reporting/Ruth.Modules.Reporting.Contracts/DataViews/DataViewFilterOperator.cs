namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Comparison operator for a DataView leaf predicate.</summary>
public enum DataViewFilterOperator
{
    /// <summary>Exact equality (case-insensitive for text).</summary>
    Equals = 0,

    /// <summary>Inequality.</summary>
    NotEquals = 1,

    /// <summary>Substring contains (case-insensitive).</summary>
    Contains = 2,

    /// <summary>Prefix match (case-insensitive).</summary>
    StartsWith = 3,

    /// <summary>Suffix match (case-insensitive).</summary>
    EndsWith = 4,

    /// <summary>Strictly greater than.</summary>
    GreaterThan = 5,

    /// <summary>Greater than or equal to.</summary>
    GreaterThanOrEqual = 6,

    /// <summary>Strictly less than.</summary>
    LessThan = 7,

    /// <summary>Less than or equal to.</summary>
    LessThanOrEqual = 8,

    /// <summary>Null or empty string check.</summary>
    IsBlank = 9,

    /// <summary>Non-null and non-empty check.</summary>
    IsNotBlank = 10,

    /// <summary>Set membership against comma-separated or JSON array values.</summary>
    In = 11,

    /// <summary>Inclusive range comparison between Value and SecondaryValue.</summary>
    Between = 12,
}
