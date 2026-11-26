using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to create a new Engagement PipelineType.</summary>
/// <param name="Spec">Pipeline type specification.</param>
public sealed record CreatePipelineTypeCommand(PipelineTypeSpec Spec)
    : ICommand<PipelineTypeResponse>;
