namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Composite conditional visibility rule governing whether a form section or field is shown.</summary>
/// <param name="MatchMode">All (AND) or Any (OR) condition evaluation.</param>
/// <param name="Conditions">Non-empty list of visibility predicates.</param>
public sealed record ConditionalVisibilityRuleDto(
    VisibilityMatchMode MatchMode,
    IReadOnlyList<FieldVisibilityConditionDto> Conditions
);
