using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Creates or appends recipients into a CommunicationFlowInstance and schedules step 0.</summary>
/// <param name="FlowId">Target flow identifier.</param>
/// <param name="Recipients">Explicit recipients to enroll.</param>
/// <param name="CohortLabel">Optional cohort label when launching a new instance.</param>
/// <param name="ExistingInstanceId">Optional existing active instance identifier to append into.</param>
/// <param name="TargetGroupId">Optional Group identifier whose active members are enrolled.</param>
public sealed record EnrollCommunicationFlowRecipientsCommand(
    Guid FlowId,
    IReadOnlyList<FlowRecipientEnrollmentInputDto>? Recipients = null,
    string? CohortLabel = null,
    Guid? ExistingInstanceId = null,
    Guid? TargetGroupId = null
) : ICommand<CommunicationFlowInstanceResponse>;
