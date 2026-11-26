using System.Globalization;
using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.SystemCommunications;

/// <summary>Transactional multi-channel system notification template supporting Email, SMS, and Push.</summary>
public sealed partial class SystemCommunication
    : AggregateRoot<SystemCommunicationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.SystemCommunication";

    private SystemCommunication() { }

    private SystemCommunication(SystemCommunicationId id, Guid systemGuid)
        : base(id)
    {
        SystemGuid = systemGuid;
    }

    public Guid SystemGuid { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public SystemCommunicationCategory Category { get; private set; }
    public string? FromName { get; private set; }
    public string? FromEmail { get; private set; }
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string HtmlBodyTemplate { get; private set; } = string.Empty;
    public string? RazorLayoutKey { get; private set; }
    public Guid? SmsFromSystemPhoneNumberId { get; private set; }
    public string? SmsMessageTemplate { get; private set; }
    public string? PushTitleTemplate { get; private set; }
    public string? PushMessageTemplate { get; private set; }
    public string? PushOpenUrlTemplate { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="SystemCommunication" /> aggregate.</summary>
    public static Result<SystemCommunication> Create(SystemCommunicationSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var id = SystemCommunicationId.Create();
        var entity = new SystemCommunication(id, spec.SystemGuid ?? id.Value)
        {
            IsSystem = spec.IsSystem,
        };
        var applied = entity.ApplySpec(spec);
        return applied.IsFailure ? Result.Failure<SystemCommunication>(applied.Errors) : entity;
    }

    /// <summary>Updates mutable fields on this <see cref="SystemCommunication" />.</summary>
    public Result Update(SystemCommunicationSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return ApplySpec(spec);
    }

    /// <summary>Verifies that this definition may be deleted.</summary>
    public Result EnsureCanDelete()
    {
        return IsSystem
            ? CommunicationDomainErrors.BuiltInSystemCommunicationProtected
            : Result.Success();
    }

    /// <summary>Normalizes and validates a system communication slug key.</summary>
    public static Result<string> NormalizeKey(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return CommunicationDomainErrors.SystemCommunicationKeyInvalid;
        }

        var normalized = rawKey.Trim().ToLower(CultureInfo.InvariantCulture);
        return KeyPattern.IsMatch(normalized)
            ? normalized
            : CommunicationDomainErrors.SystemCommunicationKeyInvalid;
    }

    private Result ApplySpec(SystemCommunicationSpec spec)
    {
        var keyResult = NormalizeKey(spec.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Errors);
        }

        var validated = ValidateCoreFields(
            spec.Title,
            spec.SubjectTemplate,
            spec.HtmlBodyTemplate,
            spec.FromEmail
        );
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Errors);
        }

        Title = spec.Title.Trim();
        Key = keyResult.Value;
        Category = spec.Category;
        FromName = CleanOptional(spec.FromName, 120);
        FromEmail = CleanOptional(spec.FromEmail, 254);
        SubjectTemplate = spec.SubjectTemplate.Trim();
        HtmlBodyTemplate = spec.HtmlBodyTemplate.Trim();
        RazorLayoutKey = CleanOptional(spec.RazorLayoutKey, 120);
        SmsFromSystemPhoneNumberId = NormalizeOptionalGuid(spec.SmsFromSystemPhoneNumberId);
        SmsMessageTemplate = CleanOptional(spec.SmsMessageTemplate, 1_600);
        PushTitleTemplate = CleanOptional(spec.PushTitleTemplate, 200);
        PushMessageTemplate = CleanOptional(spec.PushMessageTemplate, 1_000);
        PushOpenUrlTemplate = CleanOptional(spec.PushOpenUrlTemplate, 1_000);
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static Result ValidateCoreFields(
        string? title,
        string? subject,
        string? htmlBody,
        string? fromEmail
    )
    {
        return Result.Combine(
            RequireLength(title, 150, CommunicationDomainErrors.SystemCommunicationTitleInvalid),
            RequireLength(
                subject,
                500,
                CommunicationDomainErrors.SystemCommunicationSubjectInvalid
            ),
            RequireLength(
                htmlBody,
                131_072,
                CommunicationDomainErrors.SystemCommunicationBodyInvalid
            ),
            ValidateOptionalEmail(fromEmail)
        );
    }

    private static Result RequireLength(string? text, int maxLength, DomainError error)
    {
        return !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= maxLength
            ? Result.Success()
            : error;
    }

    private static Result ValidateOptionalEmail(string? fromEmail)
    {
        return string.IsNullOrWhiteSpace(fromEmail) || EmailPattern.IsMatch(fromEmail.Trim())
            ? Result.Success()
            : CommunicationDomainErrors.SystemCommunicationEmailInvalid;
    }

    private static string? CleanOptional(string? raw, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static Guid? NormalizeOptionalGuid(Guid? id)
    {
        return id.HasValue && id.Value != Guid.Empty ? id : null;
    }

    [GeneratedRegex(
        "^[a-z][a-z0-9._-]{2,99}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex KeyPattern { get; }

    [GeneratedRegex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex EmailPattern { get; }
}
