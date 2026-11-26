namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Projection of an attributed CommunicationFlowConversion record.</summary>
/// <param name="Id">Unique conversion identifier.</param>
/// <param name="FlowId">Attributed flow identifier.</param>
/// <param name="FlowInstanceId">Attributed flow instance identifier.</param>
/// <param name="FlowInstanceRecipientId">Attributed recipient enrollment identifier.</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="GoalKind">Conversion goal classification.</param>
/// <param name="StepOrderAtConversion">Step index reached when conversion occurred.</param>
/// <param name="ConversionValue">Optional monetary or numeric conversion value.</param>
/// <param name="ReferenceKey">Optional external reference key or URL.</param>
/// <param name="OccurredAtUtc">Conversion timestamp (UTC).</param>
/// <param name="Notes">Optional conversion notes.</param>
public sealed record CommunicationFlowConversionResponse(
    Guid Id,
    Guid FlowId,
    Guid FlowInstanceId,
    Guid FlowInstanceRecipientId,
    Guid? PersonId,
    FlowConversionGoalKind GoalKind,
    int StepOrderAtConversion,
    decimal? ConversionValue,
    string? ReferenceKey,
    DateTimeOffset OccurredAtUtc,
    string? Notes
);
