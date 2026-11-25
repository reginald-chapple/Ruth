namespace Ruth.Modules.Communication.Domain.Templates;

internal sealed record MergeIfNode(
    string RawConditionPath,
    IReadOnlyList<string> ConditionSegments,
    IReadOnlyList<MergeNode> ThenBranch,
    IReadOnlyList<MergeNode> ElseBranch
) : MergeNode;
