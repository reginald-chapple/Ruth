using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Query to retrieve a single PipelineType by GUID or URL slug.</summary>
/// <param name="IdOrSlug">GUID or URL slug.</param>
public sealed record GetPipelineTypeByIdOrSlugQuery(string IdOrSlug) : IQuery<PipelineTypeResponse>;
