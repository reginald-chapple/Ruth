using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Retrieves a single attribute definition by identifier.</summary>
public sealed record GetAttributeDefinitionByIdQuery(Guid AttributeDefinitionId)
    : IQuery<AttributeDefinitionResponse>;
