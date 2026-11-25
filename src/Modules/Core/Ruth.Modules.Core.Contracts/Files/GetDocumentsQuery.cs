using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists documents attached to an entity or of a given document type.</summary>
public sealed record GetDocumentsQuery(Guid? EntityId = null, Guid? DocumentTypeId = null)
    : IQuery<IReadOnlyList<DocumentResponse>>;
