namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Projection of an enrolled recipient's progression inside a CommunicationFlowInstance.</summary>
/// <param name="Id">Unique flow recipient identifier.</param>
/// <param name="FlowId">Owning flow identifier.</param>
/// <param name="FlowInstanceId">Owning flow instance identifier.</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="EmailAddress">Recipient email address.</param>
/// <param name="PhoneNumber">Recipient E.164 phone number.</param>
/// <param name="DisplayName">Recipient display name.</param>
/// <param name="Status">Current progression status.</param>
/// <param name="CurrentStepIndex">Zero-based index of the next step awaiting execution.</param>
/// <param name="CompletedStepsCount">Count of steps delivered or evaluated.</param>
/// <param name="OpenedStepsCount">Count of steps opened.</param>
/// <param name="ClickedStepsCount">Count of steps clicked.</param>
/// <param name="EnrolledAtUtc">Enrollment timestamp (UTC).</param>
/// <param name="LastStepSentAtUtc">Most recent step send timestamp (UTC).</param>
/// <param name="NextStepDueAtUtc">Next scheduled step execution timestamp (UTC).</param>
/// <param name="ExitedAtUtc">Completion or early-exit timestamp (UTC).</param>
/// <param name="ExitReason">Optional exit or failure reason.</param>
public sealed record CommunicationFlowInstanceRecipientResponse(
    Guid Id,
    Guid FlowId,
    Guid FlowInstanceId,
    Guid? PersonId,
    string? EmailAddress,
    string? PhoneNumber,
    string? DisplayName,
    FlowRecipientStatus Status,
    int CurrentStepIndex,
    int CompletedStepsCount,
    int OpenedStepsCount,
    int ClickedStepsCount,
    DateTimeOffset EnrolledAtUtc,
    DateTimeOffset? LastStepSentAtUtc,
    DateTimeOffset? NextStepDueAtUtc,
    DateTimeOffset? ExitedAtUtc,
    string? ExitReason
);
