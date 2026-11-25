using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Deletes a non-system campus unless it is the last remaining campus.</summary>
public sealed record DeleteCampusCommand(Guid CampusId) : ICommand<bool>;
