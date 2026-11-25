namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Signed or pending electronic signature document with tamper-evident verification hash.</summary>
public sealed record SignatureDocumentResponse(
    Guid Id,
    Guid SignatureDocumentTemplateId,
    string Name,
    string? DocumentKey,
    ElectronicSignatureStatus Status,
    Guid? AppliesToEntityId,
    Guid? AssignedToUserId,
    Guid? BinaryFileId,
    Guid? SignedByUserId,
    string? SignedName,
    string? SignedByEmail,
    string? SignedDocumentText,
    string? SignedClientIp,
    string? SignedClientUserAgent,
    DateTimeOffset? SignedOnUtc,
    string? SignatureVerificationHash,
    bool HasDrawnSignature,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
