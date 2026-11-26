using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.MergeTemplates;

/// <summary>Lists MergeTemplates.</summary>
/// <param name="IncludeInactive">Whether to include inactive templates.</param>
public sealed record GetMergeTemplatesQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<MergeTemplateResponse>>;
