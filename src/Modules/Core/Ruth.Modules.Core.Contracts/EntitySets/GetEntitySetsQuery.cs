using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.EntitySets;

/// <summary>Lists non-expired entity sets (optionally including expired ones).</summary>
public sealed record GetEntitySetsQuery(Guid? EntityTypeId = null, bool IncludeExpired = false)
    : IQuery<IReadOnlyList<EntitySetResponse>>;
