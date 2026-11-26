using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Touchpoints;

/// <summary>Scheduled or completed outreach Touchpoint for a ConstituentContact.</summary>
public sealed class Touchpoint
    : AggregateRoot<TouchpointId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.Touchpoint";

    private Touchpoint() { }

    private Touchpoint(TouchpointId id)
        : base(id) { }

    public Guid ConstituentContactId { get; private set; }
    public Guid OwnerPersonId { get; private set; }
    public Guid ConstituentPersonId { get; private set; }
    public TouchpointKind Kind { get; private set; }
    public TouchpointStatus Status { get; private set; }
    public TouchpointChannel Channel { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public TouchpointOutcomeSentiment? OutcomeSentiment { get; private set; }
    public string? Summary { get; private set; }
    public int SnoozeCount { get; private set; }

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

    public static Result<Touchpoint> Schedule(
        ConstituentContact contact,
        TouchpointKind kind,
        TouchpointChannel channel,
        DateOnly scheduledDate,
        string? summary = null
    )
    {
        ArgumentNullException.ThrowIfNull(contact);
        return new Touchpoint(TouchpointId.Create())
        {
            ConstituentContactId = contact.Id.Value,
            OwnerPersonId = contact.OwnerPersonId,
            ConstituentPersonId = contact.ConstituentPersonId,
            Kind = kind,
            Status = TouchpointStatus.Pending,
            Channel = channel,
            ScheduledDate = scheduledDate,
            Summary = TrimOptional(summary, 2000),
        };
    }

    public Result Complete(
        TouchpointChannel channelUsed,
        TouchpointOutcomeSentiment sentiment,
        string? summary,
        DateTimeOffset completedAtUtc
    )
    {
        if (
            Status
            is TouchpointStatus.Completed
                or TouchpointStatus.Cancelled
                or TouchpointStatus.Skipped
        )
        {
            return EngagementErrors.TouchpointAlreadyClosed;
        }

        Channel = channelUsed;
        OutcomeSentiment = sentiment;
        Summary = TrimOptional(summary, 2000) ?? Summary;
        Status = TouchpointStatus.Completed;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
        return Result.Success();
    }

    public Result Snooze(DateOnly snoozeUntilDate, string? note)
    {
        if (Status is not (TouchpointStatus.Pending or TouchpointStatus.Snoozed))
        {
            return EngagementErrors.TouchpointAlreadyClosed;
        }

        ScheduledDate = snoozeUntilDate;
        Status = TouchpointStatus.Snoozed;
        SnoozeCount++;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Summary = TrimOptional(note, 2000);
        }

        return Result.Success();
    }

    public Result Skip(string? reason)
    {
        if (Status is not (TouchpointStatus.Pending or TouchpointStatus.Snoozed))
        {
            return EngagementErrors.TouchpointAlreadyClosed;
        }

        Status = TouchpointStatus.Skipped;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Summary = TrimOptional(reason, 2000);
        }

        return Result.Success();
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
