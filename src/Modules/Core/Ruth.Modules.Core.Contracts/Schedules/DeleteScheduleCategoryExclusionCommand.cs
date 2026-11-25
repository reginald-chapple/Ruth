using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Deletes a schedule category exclusion and refreshes affected schedule dates.</summary>
public sealed record DeleteScheduleCategoryExclusionCommand(Guid ExclusionId) : ICommand<bool>;
