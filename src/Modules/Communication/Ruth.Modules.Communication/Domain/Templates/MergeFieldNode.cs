namespace Ruth.Modules.Communication.Domain.Templates;

internal sealed record MergeFieldNode(
    string RawPath,
    IReadOnlyList<string> PathSegments,
    IReadOnlyList<MergeFormatterCall> Formatters
) : MergeNode;
