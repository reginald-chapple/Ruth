namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInEvaluationResponse in the CheckIn module.</summary>
public sealed record CheckInEvaluationResponse(
    Guid ConfigurationId,
    string ConfigurationName,
    Guid ScheduleId,
    IReadOnlyList<EvaluatedCheckInAreaOptionResponse> AreaOptions
);
