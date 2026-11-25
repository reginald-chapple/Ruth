using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Queries DefinedValues matching an attribute key/value filter using PostgreSQL JSONB GIN containment.</summary>
public sealed record QueryDefinedValuesByAttributeQuery(
    Guid DefinedTypeId,
    string AttributeKey,
    string AttributeValue
) : IQuery<IReadOnlyList<DefinedValueResponse>>;
