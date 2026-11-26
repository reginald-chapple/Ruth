using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Bulk or targeted multi-recipient communication aggregate with review and batch dispatch lifecycle.</summary>
public sealed partial class Communication
    : AggregateRoot<CommunicationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.Communication";

    private List<EmailSection> _sections = [];

    private Communication() { }

    private Communication(CommunicationId id, Guid? createdByPersonId)
        : base(id)
    {
        Status = CommunicationStatus.Draft;
        CreatedByPersonId = NormalizeGuid(createdByPersonId);
    }

    public string Title { get; private set; } = string.Empty;
    public CommunicationChannel Channel { get; private set; }
    public CommunicationStatus Status { get; private set; }
    public string TopicKey { get; private set; } = "general-announcements";
    public bool IsBulk { get; private set; } = true;
    public Guid? TemplateId { get; private set; }
    public string? Subject { get; private set; }
    public string? PreheaderText { get; private set; }
    public string? FromName { get; private set; }
    public string? FromEmail { get; private set; }
    public string? ReplyToEmail { get; private set; }
    public Guid? SenderSystemPhoneNumberId { get; private set; }
    public string? RazorLayoutKey { get; private set; }
    public string? HtmlBodyOverride { get; private set; }
    public string? SmsBody { get; private set; }
    public string? PushTitle { get; private set; }
    public string? PushBody { get; private set; }
    public string? PushOpenUrl { get; private set; }
    public Guid? CreatedByPersonId { get; private set; }
    public Guid? ReviewerPersonId { get; private set; }
    public string? ApprovalNote { get; private set; }
    public DateTimeOffset? ScheduledSendAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

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

    /// <summary>Creates a new <see cref="Communication" /> in <see cref="CommunicationStatus.Draft" /> state.</summary>
    public static Result<Communication> CreateDraft(CommunicationDraftSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new Communication(CommunicationId.Create(), spec.CreatedByPersonId)
        {
            Channel = spec.Channel,
            TemplateId = NormalizeGuid(spec.TemplateId),
        };
        var updated = entity.UpdateDraft(spec);
        return updated.IsFailure ? Result.Failure<Communication>(updated.Errors) : entity;
    }

    /// <summary>Updates content on an editable (Draft or Rejected) communication.</summary>
    public Result UpdateDraft(CommunicationDraftSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var headerCheck = ValidateEditableDraftHeader(spec);
        if (headerCheck.IsFailure)
        {
            return headerCheck;
        }

        var sectionsResult = EmailSection.ValidateAndNormalize(spec.Sections);
        if (sectionsResult.IsFailure)
        {
            return Result.Failure(sectionsResult.Errors);
        }

        ApplyDraftSpec(spec, sectionsResult.Value);
        Status = CommunicationStatus.Draft;
        return Result.Success();
    }

    private Result ValidateEditableDraftHeader(CommunicationDraftSpec spec)
    {
        return Status is not (CommunicationStatus.Draft or CommunicationStatus.Rejected)
                ? BulkCommunicationErrors.CommunicationNotEditable
            : string.IsNullOrWhiteSpace(spec.Title) || spec.Title.Trim().Length > 200
                ? BulkCommunicationErrors.CommunicationTitleInvalid
            : string.IsNullOrWhiteSpace(spec.TopicKey) || !TopicRegex.IsMatch(spec.TopicKey.Trim())
                ? BulkCommunicationErrors.TopicKeyInvalid
            : Result.Success();
    }

    /// <summary>Transitions Draft or Rejected -> PendingApproval after validating content and recipients.</summary>
    public Result SubmitForApproval(int recipientCount)
    {
        if (Status is not (CommunicationStatus.Draft or CommunicationStatus.Rejected))
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        var ready = EnsureReadyForDispatch(recipientCount);
        if (ready.IsFailure)
        {
            return ready;
        }

        Status = CommunicationStatus.PendingApproval;
        return Result.Success();
    }

    /// <summary>Approves a Draft or PendingApproval communication for batch dispatch.</summary>
    public Result Approve(Guid? reviewerPersonId, string? approvalNote, int recipientCount)
    {
        if (Status is not (CommunicationStatus.Draft or CommunicationStatus.PendingApproval))
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        var ready = EnsureReadyForDispatch(recipientCount);
        if (ready.IsFailure)
        {
            return ready;
        }

        Status = CommunicationStatus.Approved;
        ReviewerPersonId = NormalizeGuid(reviewerPersonId);
        ApprovalNote = Clean(approvalNote);
        return Result.Success();
    }

    /// <summary>Rejects a PendingApproval communication with a mandatory reviewer note.</summary>
    public Result Reject(Guid? reviewerPersonId, string? reason)
    {
        if (Status != CommunicationStatus.PendingApproval)
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return BulkCommunicationErrors.RejectionReasonRequired;
        }

        Status = CommunicationStatus.Rejected;
        ReviewerPersonId = NormalizeGuid(reviewerPersonId);
        ApprovalNote = reason.Trim();
        return Result.Success();
    }

    /// <summary>Cancels a Draft, PendingApproval, Approved, or Rejected communication.</summary>
    public Result Cancel(string? reason)
    {
        if (
            Status
            is CommunicationStatus.Sending
                or CommunicationStatus.Sent
                or CommunicationStatus.Cancelled
        )
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        Status = CommunicationStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            ApprovalNote = reason.Trim();
        }

        return Result.Success();
    }

    /// <summary>Transitions Approved -> Sending when the batch job begins dispatching recipients.</summary>
    public Result BeginSending()
    {
        if (Status is not (CommunicationStatus.Approved or CommunicationStatus.Sending))
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        Status = CommunicationStatus.Sending;
        return Result.Success();
    }

    /// <summary>Transitions Sending -> Sent once all recipient attempts complete.</summary>
    public Result CompleteSending(DateTimeOffset completedAtUtc)
    {
        if (Status is not (CommunicationStatus.Sending or CommunicationStatus.Approved))
        {
            return BulkCommunicationErrors.InvalidLifecycleTransition;
        }

        Status = CommunicationStatus.Sent;
        SentAtUtc = completedAtUtc.ToUniversalTime();
        return Result.Success();
    }

    /// <summary>Repoints person references on CRM merge.</summary>
    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (CreatedByPersonId == retiredPersonId)
        {
            CreatedByPersonId = survivingPersonId;
        }

        if (ReviewerPersonId == retiredPersonId)
        {
            ReviewerPersonId = survivingPersonId;
        }
    }

    internal void ReplaceSectionsFromPersistence(List<EmailSection> sections)
    {
        _sections = sections;
    }

    private Result EnsureReadyForDispatch(int recipientCount)
    {
        return recipientCount <= 0
            ? BulkCommunicationErrors.RecipientsRequiredForApproval
            : Channel switch
            {
                CommunicationChannel.Email => EnsureEmailReady(),
                CommunicationChannel.Sms => string.IsNullOrWhiteSpace(SmsBody)
                    ? BulkCommunicationErrors.ChannelContentRequired
                    : Result.Success(),
                CommunicationChannel.Push => string.IsNullOrWhiteSpace(PushTitle)
                || string.IsNullOrWhiteSpace(PushBody)
                    ? BulkCommunicationErrors.ChannelContentRequired
                    : Result.Success(),
                _ => Result.Success(),
            };
    }

    private Result EnsureEmailReady()
    {
        return string.IsNullOrWhiteSpace(Subject) || Subject.Length > 500
                ? BulkCommunicationErrors.EmailSubjectRequired
            : _sections.Count == 0 && string.IsNullOrWhiteSpace(HtmlBodyOverride)
                ? BulkCommunicationErrors.ChannelContentRequired
            : Result.Success();
    }

    private void ApplyDraftSpec(CommunicationDraftSpec spec, IReadOnlyList<EmailSection> sections)
    {
        Title = spec.Title.Trim();
        TopicKey = spec.TopicKey.Trim();
        IsBulk = spec.IsBulk;
        Subject = Clean(spec.Subject);
        PreheaderText = Clean(spec.PreheaderText);
        FromName = Clean(spec.FromName);
        FromEmail = Clean(spec.FromEmail);
        ReplyToEmail = Clean(spec.ReplyToEmail);
        SenderSystemPhoneNumberId = NormalizeGuid(spec.SenderSystemPhoneNumberId);
        RazorLayoutKey = Clean(spec.RazorLayoutKey);
        HtmlBodyOverride = Clean(spec.HtmlBodyOverride);
        SmsBody = Clean(spec.SmsBody);
        PushTitle = Clean(spec.PushTitle);
        PushBody = Clean(spec.PushBody);
        PushOpenUrl = Clean(spec.PushOpenUrl);
        ScheduledSendAtUtc = spec.ScheduledSendAtUtc?.ToUniversalTime();
        _sections = [.. sections];
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id.HasValue && id.Value != Guid.Empty ? id : null;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{1,79}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex TopicRegex { get; }
}
