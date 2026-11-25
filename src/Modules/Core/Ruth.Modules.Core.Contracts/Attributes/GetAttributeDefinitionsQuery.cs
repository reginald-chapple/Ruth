using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Queries attribute definitions scoped to global attributes, an entity type, or an entity qualifier.</summary>
public sealed record GetAttributeDefinitionsQuery(
    Guid? EntityTypeId = null,
    string? EntityTypeKey = null,
    string? QualifierColumn = null,
    string? QualifierValue = null,
    bool GlobalOnly = false,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<AttributeDefinitionResponse>>;
