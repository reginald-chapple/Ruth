using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.SupportRequests;

/// <summary>Community Care / SupportRequest aggregate with moderation, expiration, comments, and support counter.</summary>
public sealed class SupportRequest
    : AggregateRoot<SupportRequestId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.SupportRequest";

    private readonly List<SupportRequestComment> _comments = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private SupportRequest() { }

    private SupportRequest(SupportRequestId id)
        : base(id) { }

    public Guid SupportCategoryId { get; private set; }
    public Guid? RequesterPersonId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? AssignedCareCoordinatorPersonId { get; private set; }
    public string RequestText { get; private set; } = string.Empty;
    public string? AnswerOrOutcomeSummary { get; private set; }
    public SupportVisibilityLevel VisibilityLevel { get; private set; }
    public SupportModerationState ModerationState { get; private set; }
    public bool IsUrgent { get; private set; }
    public bool AllowComments { get; private set; }
    public int CommunitySupportCount { get; private set; }
    public int FlagCount { get; private set; }
    public DateTimeOffset EnteredAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid? ApprovedByPersonId { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public IReadOnlyList<SupportRequestComment> Comments => _comments;

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();
    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalRestrictedDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalRestrictedDefaultSecured.Instance.IsAllowedByDefault(action);
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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<SupportRequest> Submit(
        SupportCategory category,
        SupportRequestIntakeSpec spec,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(spec);
        var requesterId = NormalizeGuid(spec.RequesterPersonId);
        if (!IsValidSubmission(category, spec, requesterId))
        {
            return EngagementErrors.SupportRequestInvalid;
        }

        var utc = nowUtc.ToUniversalTime();
        var autoApproved = !category.RequiresModeration;
        return new SupportRequest(SupportRequestId.Create())
        {
            SupportCategoryId = category.Id.Value,
            RequesterPersonId = requesterId,
            FirstName = spec.FirstName.Trim(),
            LastName = TrimOptional(spec.LastName, 100),
            Email = TrimOptional(spec.Email, 254),
            BranchId = NormalizeGuid(spec.BranchId),
            AssignedCareCoordinatorPersonId = category.DefaultCoordinatorPersonId,
            RequestText = spec.RequestText.Trim(),
            VisibilityLevel = CoerceVisibility(category, spec.VisibilityLevel),
            ModerationState = autoApproved
                ? SupportModerationState.Approved
                : SupportModerationState.PendingReview,
            IsUrgent = spec.IsUrgent,
            AllowComments = category.AllowComments && spec.AllowComments,
            EnteredAtUtc = utc,
            ApprovedAtUtc = autoApproved ? utc : null,
            ExpiresAtUtc = utc.AddDays(category.DefaultExpirationDays),
        };
    }

    public Result Moderate(
        SupportCategory category,
        SupportModerationDecision decision,
        Guid? moderatorPersonId,
        string? editedText,
        SupportVisibilityLevel? overrideVisibility,
        Guid? assignedCoordinatorId,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(category);
        RequestText = string.IsNullOrWhiteSpace(editedText) ? RequestText : editedText.Trim();
        VisibilityLevel = overrideVisibility.HasValue
            ? CoerceVisibility(category, overrideVisibility.Value)
            : VisibilityLevel;
        AssignedCareCoordinatorPersonId = assignedCoordinatorId.HasValue
            ? NormalizeGuid(assignedCoordinatorId)
            : AssignedCareCoordinatorPersonId;

        ModerationState = decision switch
        {
            SupportModerationDecision.Approve => SupportModerationState.Approved,
            SupportModerationDecision.Flag => SupportModerationState.Flagged,
            SupportModerationDecision.Archive => SupportModerationState.Archived,
            _ => SupportModerationState.Archived,
        };
        if (decision == SupportModerationDecision.Approve)
        {
            ApprovedAtUtc = nowUtc.ToUniversalTime();
            ApprovedByPersonId = NormalizeGuid(moderatorPersonId);
            FlagCount = 0;
        }

        return Result.Success();
    }

    public Result RecordSupport(bool isCareTeamMember, DateTimeOffset nowUtc)
    {
        var blocked =
            ModerationState == SupportModerationState.Archived
            || ExpiresAtUtc <= nowUtc.ToUniversalTime()
            || (
                !isCareTeamMember
                && (
                    ModerationState != SupportModerationState.Approved
                    || VisibilityLevel == SupportVisibilityLevel.PrivateCareTeamOnly
                )
            );
        if (blocked)
        {
            return EngagementErrors.SupportRequestActionNotAllowed;
        }

        CommunitySupportCount++;
        return Result.Success();
    }

    public void FlagForReview(int autoHoldThreshold)
    {
        FlagCount++;
        if (
            FlagCount >= Math.Max(1, autoHoldThreshold)
            && ModerationState == SupportModerationState.Approved
        )
        {
            ModerationState = SupportModerationState.Flagged;
        }
    }

    public Result AddComment(
        string authorDisplayName,
        string body,
        Guid? authorPersonId,
        bool isInternalCareNote,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(authorDisplayName) || string.IsNullOrWhiteSpace(body))
        {
            return EngagementErrors.SupportRequestInvalid;
        }

        if (
            !isInternalCareNote
            && (!AllowComments || ModerationState != SupportModerationState.Approved)
        )
        {
            return EngagementErrors.SupportRequestActionNotAllowed;
        }

        _comments.Add(
            new SupportRequestComment(
                SupportRequestCommentId.Create(),
                Id,
                authorPersonId,
                authorDisplayName,
                body,
                isInternalCareNote,
                nowUtc
            )
        );
        return Result.Success();
    }

    public void Resolve(string? outcomeSummary, bool archiveImmediately, DateTimeOffset nowUtc)
    {
        AnswerOrOutcomeSummary = TrimOptional(outcomeSummary, 2000);
        ResolvedAtUtc = nowUtc.ToUniversalTime();
        if (archiveImmediately)
        {
            ModerationState = SupportModerationState.Archived;
        }
    }

    public bool ArchiveIfExpired(DateTimeOffset nowUtc)
    {
        if (
            ModerationState == SupportModerationState.Archived
            || ExpiresAtUtc > nowUtc.ToUniversalTime()
        )
        {
            return false;
        }

        ModerationState = SupportModerationState.Archived;
        return true;
    }

    public bool RepointPerson(Guid retired, Guid surviving)
    {
        var changed = false;
        if (RequesterPersonId == retired)
        {
            RequesterPersonId = surviving;
            changed = true;
        }
        if (AssignedCareCoordinatorPersonId == retired)
        {
            AssignedCareCoordinatorPersonId = surviving;
            changed = true;
        }
        if (ApprovedByPersonId == retired)
        {
            ApprovedByPersonId = surviving;
            changed = true;
        }
        foreach (var c in _comments)
        {
            changed |= c.RepointAuthor(retired, surviving);
        }
        return changed;
    }

    private static bool IsValidSubmission(
        SupportCategory category,
        SupportRequestIntakeSpec spec,
        Guid? requesterId
    )
    {
        var hasRequiredText =
            !string.IsNullOrWhiteSpace(spec.FirstName)
            && !string.IsNullOrWhiteSpace(spec.RequestText);
        var satisfiesIdentity =
            category.AllowAnonymousIntake
            || requesterId.HasValue
            || !string.IsNullOrWhiteSpace(spec.Email);
        return hasRequiredText && satisfiesIdentity;
    }

    private static SupportVisibilityLevel CoerceVisibility(
        SupportCategory cat,
        SupportVisibilityLevel requested
    )
    {
        return
            requested == SupportVisibilityLevel.PublicCommunityWall && !cat.AllowPublicCommunityWall
            ? SupportVisibilityLevel.PrivateCareTeamOnly
            : requested;
    }
}
