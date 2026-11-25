using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Configures min/desired/max volunteer thresholds for a GroupLocation and Schedule.</summary>
public sealed record UpsertLocationScheduleConfigCommand(
    Guid GroupLocationId,
    Guid ScheduleId,
    string? ConfigurationName = null,
    int? MinimumCapacity = null,
    int? DesiredCapacity = null,
    int? MaximumCapacity = null,
    string? ConfirmationAdditionalDetails = null,
    string? ReminderAdditionalDetails = null
) : ICommand<GroupLocationScheduleConfigResponse>;
