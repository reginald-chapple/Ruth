using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Visitor or member browsing session grouping correlated <see cref="Interaction" /> events.</summary>
public sealed class InteractionSession
    : AggregateRoot<InteractionSessionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private InteractionSession() { }

    private InteractionSession(InteractionSessionId id)
        : base(id) { }

    public string SessionKey { get; private set; } = string.Empty;
    public string? DeviceType { get; private set; }
    public string? Browser { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? IpAddressHash { get; private set; }
    public Guid? PersonId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateTimeOffset StartedDateTimeUtc { get; private set; }
    public DateTimeOffset LastActivityDateTimeUtc { get; private set; }

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

    public static Result<InteractionSession> Create(
        string sessionKey,
        DateTimeOffset startedUtc,
        Guid? personId = null,
        Guid? branchId = null,
        string? deviceType = null,
        string? browser = null,
        string? operatingSystem = null,
        string? ipAddressHash = null
    )
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return Result.Failure<InteractionSession>(
                CmsErrors.Validation("InteractionSession.KeyRequired", "SessionKey is required.")
            );
        }

        var utc = startedUtc.ToUniversalTime();
        return Result.Success(
            new InteractionSession(InteractionSessionId.Create())
            {
                SessionKey = sessionKey.Trim(),
                PersonId = personId,
                BranchId = branchId,
                DeviceType = string.IsNullOrWhiteSpace(deviceType) ? null : deviceType.Trim(),
                Browser = string.IsNullOrWhiteSpace(browser) ? null : browser.Trim(),
                OperatingSystem = string.IsNullOrWhiteSpace(operatingSystem)
                    ? null
                    : operatingSystem.Trim(),
                IpAddressHash = string.IsNullOrWhiteSpace(ipAddressHash)
                    ? null
                    : ipAddressHash.Trim(),
                StartedDateTimeUtc = utc,
                LastActivityDateTimeUtc = utc,
            }
        );
    }

    public void Touch(DateTimeOffset activityUtc, Guid? personId)
    {
        var utc = activityUtc.ToUniversalTime();
        if (utc > LastActivityDateTimeUtc)
        {
            LastActivityDateTimeUtc = utc;
        }

        if (PersonId is null && personId is not null)
        {
            PersonId = personId;
        }
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
