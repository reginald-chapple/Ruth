using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Deletes a GroupType-level schedule exclusion.</summary>
public sealed record RemoveGroupScheduleExclusionCommand(Guid ExclusionId) : ICommand<bool>;
