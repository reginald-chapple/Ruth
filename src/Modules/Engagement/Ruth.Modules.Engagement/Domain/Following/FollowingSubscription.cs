using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain.Following;

/// <summary>Subscription allowing a constituent or staff member to follow entity activity digests.</summary>
public sealed class FollowingSubscription
    : AggregateRoot<FollowingSubscriptionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private FollowingSubscription() { }

    private FollowingSubscription(FollowingSubscriptionId id)
        : base(id) { }

    public Guid FollowerPersonId { get; private set; }
    public string EntityTypeKey { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string EntityDisplayName { get; private set; } = string.Empty;
    public string PurposeKey { get; private set; } = "default";
    public bool IsSubscribed { get; private set; } = true;
    public DateTimeOffset? LastNotifiedAtUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<FollowingSubscription> Create(
        Guid followerPersonId,
        string entityTypeKey,
        Guid entityId,
        string entityDisplayName,
        string? purposeKey,
        bool isSubscribed = true
    )
    {
        var invalid =
            followerPersonId == Guid.Empty
            || string.IsNullOrWhiteSpace(entityTypeKey)
            || entityId == Guid.Empty;
        return invalid
            ? EngagementErrors.FollowingInvalid
            : new FollowingSubscription(FollowingSubscriptionId.Create())
            {
                FollowerPersonId = followerPersonId,
                EntityTypeKey = entityTypeKey.Trim(),
                EntityId = entityId,
                EntityDisplayName = string.IsNullOrWhiteSpace(entityDisplayName)
                    ? entityTypeKey.Trim()
                    : entityDisplayName.Trim(),
                PurposeKey = string.IsNullOrWhiteSpace(purposeKey)
                    ? "default"
                    : purposeKey.Trim().ToLowerInvariant(),
                IsSubscribed = isSubscribed,
            };
    }

    public void SetSubscriptionState(bool isSubscribed, string? displayName)
    {
        IsSubscribed = isSubscribed;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            EntityDisplayName = displayName.Trim();
        }
    }

    public void StampNotified(DateTimeOffset notifiedAtUtc)
    {
        LastNotifiedAtUtc = notifiedAtUtc.ToUniversalTime();
    }

    public bool RepointFollower(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (FollowerPersonId != retiredPersonId)
        {
            return false;
        }

        FollowerPersonId = survivingPersonId;
        return true;
    }
}
