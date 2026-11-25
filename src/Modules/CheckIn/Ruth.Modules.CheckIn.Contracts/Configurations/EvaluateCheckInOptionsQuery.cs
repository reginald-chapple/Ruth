using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type EvaluateCheckInOptionsQuery in the CheckIn module.</summary>
public sealed record EvaluateCheckInOptionsQuery(
    Guid ConfigurationId,
    Guid ScheduleId,
    CheckInParticipantContextDto Participant,
    IReadOnlyDictionary<Guid, int>? CurrentOccupancyByLocationId = null
) : IQuery<CheckInEvaluationResponse>;
