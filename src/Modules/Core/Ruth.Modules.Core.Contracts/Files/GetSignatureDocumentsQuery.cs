using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists electronic signature documents by template or target entity.</summary>
public sealed record GetSignatureDocumentsQuery(
    Guid? TemplateId = null,
    Guid? AppliesToEntityId = null
) : IQuery<IReadOnlyList<SignatureDocumentResponse>>;
