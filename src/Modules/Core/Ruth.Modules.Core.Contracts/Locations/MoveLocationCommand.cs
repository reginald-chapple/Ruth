using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Moves a location to a new parent location after validating no circular hierarchy is created.</summary>
public sealed record MoveLocationCommand(Guid LocationId, Guid? NewParentLocationId)
    : ICommand<LocationResponse>;
