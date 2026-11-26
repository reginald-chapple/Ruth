using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Executes one bounded drip advancement pass across due CommunicationFlowInstanceRecipient steps.</summary>
/// <param name="MaxRecipientsPerRun">Maximum due recipients to advance in this pass (1..500).</param>
/// <param name="SpecificFlowId">Optional flow filter.</param>
/// <param name="EvaluationTimeOverrideUtc">Optional simulated evaluation timestamp (UTC) for testing timed delays.</param>
public sealed record ProcessDueCommunicationFlowsCommand(
    int MaxRecipientsPerRun = 200,
    Guid? SpecificFlowId = null,
    DateTimeOffset? EvaluationTimeOverrideUtc = null
) : ICommand<FlowAdvancementBatchResultResponse>;
