namespace Ruth.Modules.Communication.Domain.Templates;

internal sealed record ParsedMergeTemplate(IReadOnlyList<MergeNode> Nodes, int TokenCount);
