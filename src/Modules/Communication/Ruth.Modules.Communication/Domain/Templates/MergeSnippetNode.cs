namespace Ruth.Modules.Communication.Domain.Templates;

internal sealed record MergeSnippetNode(
    string SnippetKey,
    IReadOnlyDictionary<string, MergeArgumentValue> Arguments
) : MergeNode;
