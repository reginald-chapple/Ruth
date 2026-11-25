namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Represents a schedule assignment on a campus.</summary>
public sealed record CampusScheduleDto(Guid ScheduleId, Guid? ScheduleTypeValueId, int Order);
