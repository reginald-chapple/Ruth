using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Reusable bulk communication template aggregate with structured email composer sections.</summary>
public sealed partial class CommunicationTemplate
    : AggregateRoot<CommunicationTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.CommunicationTemplate";

    private List<EmailSection> _sections = [];

    private CommunicationTemplate() { }

    private CommunicationTemplate(CommunicationTemplateId id, Guid systemGuid, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        IsSystem = isSystem;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CommunicationChannel Channel { get; private set; }
    public string TopicKey { get; private set; } = "general-announcements";
    public string? SubjectTemplate { get; private set; }
    public string? PreheaderText { get; private set; }
    public string? FromName { get; private set; }
    public string? FromEmail { get; private set; }
    public string? ReplyToEmail { get; private set; }
    public Guid? SenderSystemPhoneNumberId { get; private set; }
    public string? RazorLayoutKey { get; private set; }
    public string? SmsBodyTemplate { get; private set; }
    public string? PushTitleTemplate { get; private set; }
    public string? PushBodyTemplate { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<EmailSection> Sections => _sections;

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

    /// <summary>Creates a validated <see cref="CommunicationTemplate" /> aggregate.</summary>
    public static Result<CommunicationTemplate> Create(CommunicationTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Key) || !SlugRegex.IsMatch(spec.Key.Trim()))
        {
            return Result.Failure<CommunicationTemplate>(
                BulkCommunicationErrors.TemplateKeyInvalid
            );
        }

        var id = CommunicationTemplateId.Create();
        var template = new CommunicationTemplate(id, spec.SystemGuid ?? id.Value, spec.IsSystem)
        {
            Key = spec.Key.Trim(),
        };

        var updated = template.Update(spec);
        return updated.IsFailure ? Result.Failure<CommunicationTemplate>(updated.Errors) : template;
    }

    /// <summary>Updates mutable fields and structured email sections on this template.</summary>
    public Result Update(CommunicationTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Name) || spec.Name.Trim().Length > 150)
        {
            return BulkCommunicationErrors.TemplateNameInvalid;
        }

        if (string.IsNullOrWhiteSpace(spec.TopicKey) || !TopicRegex.IsMatch(spec.TopicKey.Trim()))
        {
            return BulkCommunicationErrors.TopicKeyInvalid;
        }

        var sectionsResult = EmailSection.ValidateAndNormalize(spec.Sections);
        if (sectionsResult.IsFailure)
        {
            return Result.Failure(sectionsResult.Errors);
        }

        var contentCheck = ValidateChannelRequirements(spec, sectionsResult.Value);
        if (contentCheck.IsFailure)
        {
            return contentCheck;
        }

        ApplyFields(spec, sectionsResult.Value);
        return Result.Success();
    }

    /// <summary>Validates whether this template is allowed to be deleted.</summary>
    public Result EnsureCanDelete()
    {
        return IsSystem ? BulkCommunicationErrors.SystemTemplateProtected : Result.Success();
    }

    internal void ReplaceSectionsFromPersistence(List<EmailSection> sections)
    {
        _sections = sections;
    }

    private static Result ValidateChannelRequirements(
        CommunicationTemplateSpec spec,
        IReadOnlyList<EmailSection> sections
    )
    {
        return spec.Channel switch
        {
            CommunicationChannel.Email => ValidateEmailRequirements(
                spec.SubjectTemplate,
                sections.Count
            ),
            CommunicationChannel.Sms => string.IsNullOrWhiteSpace(spec.SmsBodyTemplate)
                ? BulkCommunicationErrors.ChannelContentRequired
                : Result.Success(),
            CommunicationChannel.Push => string.IsNullOrWhiteSpace(spec.PushTitleTemplate)
            || string.IsNullOrWhiteSpace(spec.PushBodyTemplate)
                ? BulkCommunicationErrors.ChannelContentRequired
                : Result.Success(),
            _ => Result.Success(),
        };
    }

    private static Result ValidateEmailRequirements(string? subject, int sectionCount)
    {
        return string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 500
                ? BulkCommunicationErrors.EmailSubjectRequired
            : sectionCount == 0 ? BulkCommunicationErrors.ChannelContentRequired
            : Result.Success();
    }

    private void ApplyFields(CommunicationTemplateSpec spec, IReadOnlyList<EmailSection> sections)
    {
        Name = spec.Name.Trim();
        Description = Clean(spec.Description);
        Channel = spec.Channel;
        TopicKey = spec.TopicKey.Trim();
        SubjectTemplate = Clean(spec.SubjectTemplate);
        PreheaderText = Clean(spec.PreheaderText);
        FromName = Clean(spec.FromName);
        FromEmail = Clean(spec.FromEmail);
        ReplyToEmail = Clean(spec.ReplyToEmail);
        SenderSystemPhoneNumberId =
            spec.SenderSystemPhoneNumberId.HasValue
            && spec.SenderSystemPhoneNumberId.Value != Guid.Empty
                ? spec.SenderSystemPhoneNumberId
                : null;
        RazorLayoutKey = Clean(spec.RazorLayoutKey);
        SmsBodyTemplate = Clean(spec.SmsBodyTemplate);
        PushTitleTemplate = Clean(spec.PushTitleTemplate);
        PushBodyTemplate = Clean(spec.PushBodyTemplate);
        IsActive = spec.IsActive;
        _sections = [.. sections];
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{2,99}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{1,79}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex TopicRegex { get; }
}
