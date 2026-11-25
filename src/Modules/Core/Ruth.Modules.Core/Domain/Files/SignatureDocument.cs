using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Electronic signature document with tamper-evident SHA-256 verification hash and post-signing immutability.</summary>
public sealed partial class SignatureDocument
    : AggregateRoot<SignatureDocumentId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for SignatureDocument.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.SignatureDocument";

    private SignatureDocument() { }

    private SignatureDocument(
        SignatureDocumentId id,
        SignatureDocumentTemplateId templateId,
        string name,
        Guid? appliesToEntityId,
        Guid? assignedToUserId,
        string? documentKey,
        string? renderedDocumentText
    )
        : base(id)
    {
        SignatureDocumentTemplateId = templateId;
        Name = name;
        AppliesToEntityId = appliesToEntityId;
        AssignedToUserId = assignedToUserId;
        DocumentKey = documentKey;
        SignedDocumentText = renderedDocumentText;
        Status = ElectronicSignatureStatus.Sent;
    }

    /// <summary>Gets the owning template identifier.</summary>
    public SignatureDocumentTemplateId SignatureDocumentTemplateId { get; private set; }

    /// <summary>Gets the document title.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional external reference key.</summary>
    public string? DocumentKey { get; private set; }

    /// <summary>Gets the current signing lifecycle status.</summary>
    public ElectronicSignatureStatus Status { get; private set; }

    /// <summary>Gets the optional target entity/person identifier this agreement applies to.</summary>
    public Guid? AppliesToEntityId { get; private set; }

    /// <summary>Gets the optional user assigned to sign.</summary>
    public Guid? AssignedToUserId { get; private set; }

    /// <summary>Gets the optional archived binary file identifier.</summary>
    public BinaryFileId? BinaryFileId { get; private set; }

    /// <summary>Gets the user who signed the document.</summary>
    public Guid? SignedByUserId { get; private set; }

    /// <summary>Gets the signer's typed legal name.</summary>
    public string? SignedName { get; private set; }

    /// <summary>Gets the signer's email address.</summary>
    public string? SignedByEmail { get; private set; }

    /// <summary>Gets the exact rendered document text frozen at signing time.</summary>
    public string? SignedDocumentText { get; private set; }

    /// <summary>Gets the client IP recorded at signing time.</summary>
    public string? SignedClientIp { get; private set; }

    /// <summary>Gets the client User-Agent recorded at signing time.</summary>
    public string? SignedClientUserAgent { get; private set; }

    /// <summary>Gets the UTC timestamp when the document was signed.</summary>
    public DateTimeOffset? SignedOnUtc { get; private set; }

    /// <summary>Gets the drawn PNG data URI when <see cref="ElectronicSignatureType.Drawn" /> was used.</summary>
    public string? SignatureData { get; private set; }

    /// <summary>Gets the tamper-evident verification hash computed upon signing.</summary>
    public string? SignatureVerificationHash { get; private set; }

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
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            SignatureDocumentTemplate.EntityTypeKeyName,
            SignatureDocumentTemplateId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Gets whether this document has been cryptographically sealed.</summary>
    public bool IsSealed => !string.IsNullOrWhiteSpace(SignatureVerificationHash);

    /// <summary>Creates a new pending signature document.</summary>
    public static Result<SignatureDocument> Create(
        SignatureDocumentTemplateId templateId,
        string name,
        Guid? appliesToEntityId = null,
        Guid? assignedToUserId = null,
        string? documentKey = null,
        string? renderedDocumentText = null
    )
    {
        if (templateId == SignatureDocumentTemplateId.Empty)
        {
            return CoreDocumentErrors.SignatureTemplateNotFound;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            maxLength: 250,
            CoreDocumentErrors.DocumentNameRequired,
            CoreDocumentErrors.BinaryFileTypeNameTooLong
        );
        return nameResult.IsFailure
            ? Result.Failure<SignatureDocument>(nameResult.Errors)
            : (Result<SignatureDocument>)
                new SignatureDocument(
                    SignatureDocumentId.Create(),
                    templateId,
                    nameResult.Value,
                    appliesToEntityId,
                    assignedToUserId,
                    string.IsNullOrWhiteSpace(documentKey) ? null : documentKey.Trim(),
                    renderedDocumentText
                );
    }

    /// <summary>Captures the electronic signature, validates drawn PNG input, and seals the document immutably.</summary>
    public Result Sign(
        ElectronicSignatureType requiredType,
        string signedName,
        string? signedByEmail,
        Guid? signedByUserId,
        string? drawnSignatureDataUrl,
        string? signedDocumentText,
        string? clientIp,
        string? clientUserAgent,
        DateTimeOffset signedOnUtc
    )
    {
        if (IsSealed || Status == ElectronicSignatureStatus.Completed)
        {
            return CoreDocumentErrors.SignatureDocumentAlreadySigned;
        }

        if (string.IsNullOrWhiteSpace(signedName) || signedName.Trim().Length > 250)
        {
            return CoreDocumentErrors.SignatureSignedNameRequired;
        }

        string? normalizedSignatureData = null;
        if (requiredType == ElectronicSignatureType.Drawn)
        {
            if (
                string.IsNullOrWhiteSpace(drawnSignatureDataUrl)
                || !PngDataUriRegex.IsMatch(drawnSignatureDataUrl.Trim())
            )
            {
                return CoreDocumentErrors.SignatureDrawnDataInvalid;
            }

            normalizedSignatureData = drawnSignatureDataUrl.Trim();
        }

        var roundedSignedOn = TruncateToMilliseconds(signedOnUtc.ToUniversalTime());
        SignedName = signedName.Trim();
        SignedByEmail = string.IsNullOrWhiteSpace(signedByEmail) ? null : signedByEmail.Trim();
        SignedByUserId = signedByUserId;
        SignedDocumentText = signedDocumentText ?? SignedDocumentText ?? string.Empty;
        SignedClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp.Trim();
        SignedClientUserAgent = string.IsNullOrWhiteSpace(clientUserAgent)
            ? null
            : clientUserAgent.Trim();
        SignedOnUtc = roundedSignedOn;
        SignatureData = normalizedSignatureData;
        Status = ElectronicSignatureStatus.Completed;
        SignatureVerificationHash = ComputeVerificationHash();
        return Result.Success();
    }

    /// <summary>Recomputes the verification hash over the sealed evidence fields to detect tampering.</summary>
    public bool VerifyIntegrity()
    {
        return IsSealed
            && string.Equals(
                SignatureVerificationHash,
                ComputeVerificationHash(),
                StringComparison.Ordinal
            );
    }

    private string ComputeVerificationHash()
    {
        var isoTime = SignedOnUtc?.ToString(
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture
        ) ?? string.Empty;
        var payload = string.Join(
            '|',
            SignedDocumentText ?? string.Empty,
            SignedClientIp ?? string.Empty,
            SignedClientUserAgent ?? string.Empty,
            isoTime,
            SignedByUserId?.ToString("D") ?? string.Empty,
            SignatureData ?? string.Empty,
            SignedName ?? string.Empty
        );
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"B{Convert.ToHexStringLower(hashBytes)}";
    }

    private static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value)
    {
        var ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTimeOffset(ticks, value.Offset);
    }

    [GeneratedRegex(
        @"^data:image/png;base64,[A-Za-z0-9+/]+={0,2}\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex PngDataUriRegex { get; }
}
