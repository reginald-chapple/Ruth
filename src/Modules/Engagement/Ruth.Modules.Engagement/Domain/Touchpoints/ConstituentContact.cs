using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Touchpoints;

/// <summary>Personal portfolio constituent assignment managed by a steward, advisor, or caseworker.</summary>
public sealed class ConstituentContact
    : AggregateRoot<ConstituentContactId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.ConstituentContact";

    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private ConstituentContact() { }

    private ConstituentContact(ConstituentContactId id)
        : base(id) { }

    public Guid OwnerPersonId { get; private set; }
    public Guid ConstituentPersonId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string PortfolioRole { get; private set; } = "Steward";
    public ContactRelationshipStrength RelationshipStrength { get; private set; }
    public int OutreachCadenceDays { get; private set; } = 30;
    public int? ConnectionNoteCadenceDays { get; private set; }
    public TouchpointChannel PreferredChannel { get; private set; }
    public DateTimeOffset? LastTouchpointAtUtc { get; private set; }
    public DateTimeOffset? LastConnectionNoteAtUtc { get; private set; }
    public TouchpointOutcomeSentiment? LatestSentiment { get; private set; }
    public DateOnly? NextScheduledTouchpointDate { get; private set; }
    public int HealthScore { get; private set; }
    public ConstituentHealthStatus HealthStatus { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<ConstituentContact> Create(ConstituentContactSpec spec, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new ConstituentContact(ConstituentContactId.Create());
        var res = entity.ApplySpec(spec, today);
        return res.IsFailure ? Result.Failure<ConstituentContact>(res.Errors) : entity;
    }

    public Result ApplySpec(ConstituentContactSpec spec, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (!HasValidParticipants(spec) || !HasValidCadences(spec))
        {
            return EngagementErrors.ConstituentContactInvalid;
        }

        OwnerPersonId = spec.OwnerPersonId;
        ConstituentPersonId = spec.ConstituentPersonId;
        BranchId = NormalizeGuid(spec.BranchId);
        PortfolioRole = spec.PortfolioRole.Trim();
        RelationshipStrength = spec.RelationshipStrength;
        OutreachCadenceDays = spec.OutreachCadenceDays;
        ConnectionNoteCadenceDays = spec.ConnectionNoteCadenceDays;
        PreferredChannel = spec.PreferredChannel;
        Notes = TrimOptional(spec.Notes, 2000);
        IsActive = spec.IsActive;
        ApplyAttributes(_attributeValues, spec.Attributes);
        NextScheduledTouchpointDate = null;
        RefreshScheduleAndHealth(today);
        return Result.Success();
    }

    private static bool HasValidParticipants(ConstituentContactSpec spec)
    {
        return spec.OwnerPersonId != Guid.Empty
            && spec.ConstituentPersonId != Guid.Empty
            && spec.OwnerPersonId != spec.ConstituentPersonId
            && !string.IsNullOrWhiteSpace(spec.PortfolioRole);
    }

    private static bool HasValidCadences(ConstituentContactSpec spec)
    {
        return spec.OutreachCadenceDays is >= 0 and <= 730
            && (
                !spec.ConnectionNoteCadenceDays.HasValue
                || spec.ConnectionNoteCadenceDays.Value is >= 1 and <= 730
            );
    }

    public void RecordCompletedTouchpoint(
        TouchpointKind kind,
        TouchpointChannel channel,
        TouchpointOutcomeSentiment sentiment,
        ContactRelationshipStrength? updatedStrength,
        DateTimeOffset completedAtUtc,
        DateOnly today
    )
    {
        var utc = completedAtUtc.ToUniversalTime();
        LastTouchpointAtUtc = utc;
        if (kind == TouchpointKind.ConnectionNote || channel == TouchpointChannel.HandwrittenNote)
        {
            LastConnectionNoteAtUtc = utc;
        }

        LatestSentiment = sentiment;
        if (updatedStrength.HasValue)
        {
            RelationshipStrength = updatedStrength.Value;
        }

        RefreshScheduleAndHealth(today);
    }

    public void AdvanceSkippedCycle(DateOnly skippedFromDate, DateOnly today)
    {
        if (OutreachCadenceDays > 0)
        {
            NextScheduledTouchpointDate = skippedFromDate.AddDays(OutreachCadenceDays);
        }

        var (score, status) = TouchpointCadenceScheduler.EvaluateHealth(
            IsActive,
            RelationshipStrength,
            OutreachCadenceDays,
            NextScheduledTouchpointDate,
            LatestSentiment,
            today
        );
        HealthScore = score;
        HealthStatus = status;
    }

    public void RefreshScheduleAndHealth(DateOnly today)
    {
        if (OutreachCadenceDays <= 0)
        {
            NextScheduledTouchpointDate = null;
        }
        else if (
            LastTouchpointAtUtc.HasValue
            || CreatedOnUtc != default
            || !NextScheduledTouchpointDate.HasValue
        )
        {
            var baseline =
                CreatedOnUtc == default ? today : DateOnly.FromDateTime(CreatedOnUtc.UtcDateTime);
            NextScheduledTouchpointDate = TouchpointCadenceScheduler.ComputeNextOutreachDate(
                OutreachCadenceDays,
                LastTouchpointAtUtc,
                baseline
            );
        }

        var (score, status) = TouchpointCadenceScheduler.EvaluateHealth(
            IsActive,
            RelationshipStrength,
            OutreachCadenceDays,
            NextScheduledTouchpointDate,
            LatestSentiment,
            today
        );
        HealthScore = score;
        HealthStatus = status;
    }

    public bool RepointPerson(Guid retired, Guid surviving)
    {
        var changed = false;
        if (OwnerPersonId == retired && ConstituentPersonId != surviving)
        {
            OwnerPersonId = surviving;
            changed = true;
        }

        if (ConstituentPersonId == retired && OwnerPersonId != surviving)
        {
            ConstituentPersonId = surviving;
            changed = true;
        }

        return changed;
    }
}
