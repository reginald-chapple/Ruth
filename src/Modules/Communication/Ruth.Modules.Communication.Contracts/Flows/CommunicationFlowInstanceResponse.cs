namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Projection of a CommunicationFlowInstance cohort run and its enrolled recipients.</summary>
/// <param name="Id">Unique instance identifier.</param>
/// <param name="FlowId">Owning flow identifier.</param>
/// <param name="CohortLabel">Cohort or wave label.</param>
/// <param name="Status">Instance status.</param>
/// <param name="StartDateUtc">Instance start timestamp (UTC).</param>
/// <param name="CompletedAtUtc">Completion timestamp (UTC).</param>
/// <param name="Recipients">Enrolled recipients in this instance.</param>
public sealed record CommunicationFlowInstanceResponse(
    Guid Id,
    Guid FlowId,
    string CohortLabel,
    FlowInstanceStatus Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<CommunicationFlowInstanceRecipientResponse> Recipients
);
