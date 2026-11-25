using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Lists all attribute matrix templates.</summary>
public sealed record GetAttributeMatrixTemplatesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<AttributeMatrixTemplateResponse>>;
