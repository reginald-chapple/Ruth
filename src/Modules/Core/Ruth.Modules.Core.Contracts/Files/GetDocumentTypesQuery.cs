using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists document types optionally filtered by entity type.</summary>
public sealed record GetDocumentTypesQuery(Guid? EntityTypeId = null)
    : IQuery<IReadOnlyList<DocumentTypeResponse>>;
