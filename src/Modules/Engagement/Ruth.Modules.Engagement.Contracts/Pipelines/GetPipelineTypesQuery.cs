using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to list configured Engagement PipelineTypes.</summary>
/// <param name="ActiveOnly">Whether to return only active pipelines.</param>
public sealed record GetPipelineTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<PipelineTypeResponse>>;
