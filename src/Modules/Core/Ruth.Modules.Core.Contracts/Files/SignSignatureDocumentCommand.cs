using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Captures an electronic signature, seals the document with a verification hash, and locks it against mutation.</summary>
public sealed record SignSignatureDocumentCommand(
    Guid SignatureDocumentId,
    string SignedName,
    string? SignedByEmail = null,
    Guid? SignedByUserId = null,
    string? DrawnSignatureDataUrl = null,
    string? SignedDocumentText = null,
    string? ClientIp = null,
    string? ClientUserAgent = null
) : ICommand<SignatureDocumentResponse>;
