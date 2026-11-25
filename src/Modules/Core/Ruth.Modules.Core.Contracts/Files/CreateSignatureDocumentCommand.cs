using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Creates a pending electronic signature document from a template.</summary>
public sealed record CreateSignatureDocumentCommand(
    Guid SignatureDocumentTemplateId,
    string Name,
    Guid? AppliesToEntityId = null,
    Guid? AssignedToUserId = null,
    string? DocumentKey = null,
    string? RenderedDocumentText = null
) : ICommand<SignatureDocumentResponse>;
