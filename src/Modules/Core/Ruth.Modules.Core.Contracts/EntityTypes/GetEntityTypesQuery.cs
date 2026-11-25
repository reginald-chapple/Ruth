using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.EntityTypes;

/// <summary>Lists registered entity types optionally filtered by capability.</summary>
public sealed record GetEntityTypesQuery(
    bool? OnlySecured = null,
    bool? OnlySupportingAttributes = null
) : IQuery<IReadOnlyList<EntityTypeResponse>>;
