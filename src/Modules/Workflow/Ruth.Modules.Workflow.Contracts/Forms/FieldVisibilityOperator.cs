namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Comparison operator for a conditional form field/section visibility predicate.</summary>
public enum FieldVisibilityOperator
{
    /// <summary>Case-insensitive ordinal equality.</summary>
    Equals = 0,

    /// <summary>Case-insensitive ordinal inequality.</summary>
    NotEquals = 1,

    /// <summary>Case-insensitive substring match.</summary>
    Contains = 2,

    /// <summary>Source value is null or whitespace.</summary>
    IsEmpty = 3,

    /// <summary>Source value is non-whitespace.</summary>
    IsNotEmpty = 4,

    /// <summary>Numeric decimal greater-than.</summary>
    GreaterThan = 5,

    /// <summary>Numeric decimal less-than.</summary>
    LessThan = 6,

    /// <summary>Boolean truthy check (true, 1, yes).</summary>
    IsTrue = 7,

    /// <summary>Boolean falsy check (false, 0, no, or empty).</summary>
    IsFalse = 8,
}
