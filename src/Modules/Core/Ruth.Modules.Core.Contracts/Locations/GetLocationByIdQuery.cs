using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Gets a single location by identifier.</summary>
public sealed record GetLocationByIdQuery(Guid LocationId) : IQuery<LocationResponse>;
