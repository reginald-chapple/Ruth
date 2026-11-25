namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Min/Desired/Max volunteer thresholds for a GroupLocation + Schedule slot.</summary>
public sealed record GroupLocationScheduleConfigResponse(
    Guid Id,
    Guid GroupLocationId,
    Guid GroupId,
    Guid LocationId,
    string LocationName,
    Guid ScheduleId,
    string? ConfigurationName,
    int? MinimumCapacity,
    int? DesiredCapacity,
    int? MaximumCapacity,
    string? ConfirmationAdditionalDetails,
    string? ReminderAdditionalDetails
);
