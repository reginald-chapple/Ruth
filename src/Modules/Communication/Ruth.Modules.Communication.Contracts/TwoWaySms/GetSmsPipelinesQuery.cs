using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Lists configured SmsPipeline keyword responders.</summary>
/// <param name="ActiveOnly">Whether to include only active pipelines.</param>
public sealed record GetSmsPipelinesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<SmsPipelineResponse>>;
