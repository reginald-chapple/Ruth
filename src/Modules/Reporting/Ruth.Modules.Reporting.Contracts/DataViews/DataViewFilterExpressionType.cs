namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Logical combinator or leaf node kind in a DataView Query AST.</summary>
public enum DataViewFilterExpressionType
{
    /// <summary>Leaf predicate rule.</summary>
    Rule = 0,

    /// <summary>Logical AND group (all children must match).</summary>
    GroupAll = 1,

    /// <summary>Logical OR group (at least one child must match).</summary>
    GroupAny = 2,

    /// <summary>Logical NOR group (none of the children may match).</summary>
    GroupNone = 3,

    /// <summary>Logical NAND group (not all children may match).</summary>
    GroupAllFalse = 4,
}
