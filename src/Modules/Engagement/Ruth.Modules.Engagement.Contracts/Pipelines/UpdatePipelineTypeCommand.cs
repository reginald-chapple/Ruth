using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to update an existing Engagement PipelineType.</summary>
/// <param name="PipelineTypeId">PipelineType identifier.</param>
/// <param name="Spec">Updated pipeline specification.</param>
public sealed record UpdatePipelineTypeCommand(Guid PipelineTypeId, PipelineTypeSpec Spec)
    : ICommand<PipelineTypeResponse>;
