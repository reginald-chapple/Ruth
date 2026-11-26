using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Queries configured punch pass or credit pack types.</summary>
/// <param name="ActiveOnly">When true, returns only active pass types.</param>
public sealed record GetPunchPassTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<PunchPassTypeResponse>>;
