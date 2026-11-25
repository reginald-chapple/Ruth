namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Represents a schedule assignment on a branch or site.</summary>
public sealed record BranchScheduleDto(Guid ScheduleId, Guid? ScheduleTypeValueId, int Order);
