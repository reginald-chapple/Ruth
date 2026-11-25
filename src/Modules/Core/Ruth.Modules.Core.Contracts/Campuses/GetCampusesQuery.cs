using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Lists campuses ordered by display order and name.</summary>
public sealed record GetCampusesQuery(bool ActiveOnly = false, string? SearchTerm = null)
    : IQuery<IReadOnlyList<CampusResponse>>;
