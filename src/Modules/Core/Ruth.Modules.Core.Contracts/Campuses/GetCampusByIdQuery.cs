using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Gets a single campus by identifier.</summary>
public sealed record GetCampusByIdQuery(Guid CampusId) : IQuery<CampusResponse>;
