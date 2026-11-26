using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Deletes a non-system SmsPipeline.</summary>
/// <param name="PipelineId">Target pipeline identifier.</param>
public sealed record DeleteSmsPipelineCommand(Guid PipelineId) : ICommand<bool>;
