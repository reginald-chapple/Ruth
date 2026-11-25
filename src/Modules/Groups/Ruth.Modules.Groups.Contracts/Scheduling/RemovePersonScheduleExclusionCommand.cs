using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Deletes a personal blackout date range.</summary>
public sealed record RemovePersonScheduleExclusionCommand(Guid ExclusionId) : ICommand<bool>;
