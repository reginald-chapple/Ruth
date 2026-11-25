using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Creates an electronic signature document template.</summary>
public sealed record CreateSignatureDocumentTemplateCommand(
    string Name,
    string? Description = null,
    Guid? BinaryFileTypeId = null,
    string? DocumentTerm = "Document",
    string? LavaTemplate = null,
    ElectronicSignatureType SignatureType = ElectronicSignatureType.Typed,
    bool IsActive = true,
    bool IsValidInFuture = false,
    int? ValidityDurationInDays = null
) : ICommand<SignatureDocumentTemplateResponse>;
