using Ruth.Modules.Core.Contracts.Files;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Template defining an electronically signable agreement, release, or waiver.</summary>
public sealed class SignatureDocumentTemplate
    : AggregateRoot<SignatureDocumentTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for SignatureDocumentTemplate.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.SignatureDocumentTemplate";

    private SignatureDocumentTemplate() { }

    private SignatureDocumentTemplate(
        SignatureDocumentTemplateId id,
        Guid systemGuid,
        string name,
        string? description,
        BinaryFileTypeId? binaryFileTypeId,
        string? documentTerm,
        string? lavaTemplate,
        ElectronicSignatureType signatureType,
        bool isActive,
        bool isValidInFuture,
        int? validityDurationInDays
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Name = name;
        Description = description;
        BinaryFileTypeId = binaryFileTypeId;
        DocumentTerm = documentTerm;
        LavaTemplate = lavaTemplate;
        SignatureType = signatureType;
        IsActive = isActive;
        IsValidInFuture = isValidInFuture;
        ValidityDurationInDays = validityDurationInDays is > 0 ? validityDurationInDays : null;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the template display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional binary file type used for archived PDFs.</summary>
    public BinaryFileTypeId? BinaryFileTypeId { get; private set; }

    /// <summary>Gets the display noun for this document (e.g., Waiver, Release).</summary>
    public string? DocumentTerm { get; private set; }

    /// <summary>Gets the template body markup.</summary>
    public string? LavaTemplate { get; private set; }

    /// <summary>Gets whether the signer types or draws their signature.</summary>
    public ElectronicSignatureType SignatureType { get; private set; }

    /// <summary>Gets whether the template is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether a previously signed document remains valid for future events.</summary>
    public bool IsValidInFuture { get; private set; }

    /// <summary>Gets the optional validity duration in days when <see cref="IsValidInFuture" /> is true.</summary>
    public int? ValidityDurationInDays { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Evaluates whether a document signed at <paramref name="signedOnUtc" /> is still valid as of <paramref name="asOfUtc" />.</summary>
    public bool IsSignatureValidAsOf(DateTimeOffset signedOnUtc, DateTimeOffset asOfUtc)
    {
        if (!IsValidInFuture)
        {
            return false;
        }

        if (!ValidityDurationInDays.HasValue)
        {
            return signedOnUtc <= asOfUtc;
        }

        var earliestValid = asOfUtc.Date.AddDays(-(ValidityDurationInDays.Value - 1));
        return signedOnUtc.UtcDateTime.Date >= earliestValid;
    }

    /// <summary>Creates a validated signature document template.</summary>
    public static Result<SignatureDocumentTemplate> Create(
        string name,
        string? description = null,
        BinaryFileTypeId? binaryFileTypeId = null,
        string? documentTerm = "Document",
        string? lavaTemplate = null,
        ElectronicSignatureType signatureType = ElectronicSignatureType.Typed,
        bool isActive = true,
        bool isValidInFuture = false,
        int? validityDurationInDays = null
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreDocumentErrors.SignatureTemplateNameRequired,
            CoreDocumentErrors.BinaryFileTypeNameTooLong
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<SignatureDocumentTemplate>(nameResult.Errors);
        }

        var id = SignatureDocumentTemplateId.Create();
        return new SignatureDocumentTemplate(
            id,
            id.Value,
            nameResult.Value,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            binaryFileTypeId,
            string.IsNullOrWhiteSpace(documentTerm) ? "Document" : documentTerm.Trim(),
            lavaTemplate,
            signatureType,
            isActive,
            isValidInFuture,
            validityDurationInDays
        );
    }
}
