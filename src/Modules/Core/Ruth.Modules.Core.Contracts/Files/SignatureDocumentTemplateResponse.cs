namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Template defining an electronically signable waiver, agreement, or policy document.</summary>
public sealed record SignatureDocumentTemplateResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string? Description,
    Guid? BinaryFileTypeId,
    string? DocumentTerm,
    string? TemplateMarkup,
    ElectronicSignatureType SignatureType,
    bool IsActive,
    bool IsValidInFuture,
    int? ValidityDurationInDays,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
