using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Deletes a location that has no child locations.</summary>
public sealed record DeleteLocationCommand(Guid LocationId) : ICommand<bool>;
