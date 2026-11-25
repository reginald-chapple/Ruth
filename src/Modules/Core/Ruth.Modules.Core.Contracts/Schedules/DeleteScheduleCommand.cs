using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Deletes a schedule and its materialized occurrence dates.</summary>
public sealed record DeleteScheduleCommand(Guid ScheduleId) : ICommand<bool>;
