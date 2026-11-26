namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Logical combinator applied across conditional visibility predicates.</summary>
public enum VisibilityMatchMode
{
    /// <summary>All predicates must match (logical AND).</summary>
    All = 0,

    /// <summary>At least one predicate must match (logical OR).</summary>
    Any = 1,
}
