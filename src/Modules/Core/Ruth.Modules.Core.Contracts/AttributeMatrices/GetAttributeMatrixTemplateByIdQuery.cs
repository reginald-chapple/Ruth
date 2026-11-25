using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Retrieves an attribute matrix template by identifier.</summary>
public sealed record GetAttributeMatrixTemplateByIdQuery(Guid TemplateId)
    : IQuery<AttributeMatrixTemplateResponse>;
