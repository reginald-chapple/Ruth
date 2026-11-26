using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>TTL seat-hold session preventing overselling while a registrar completes the multi-step Registration Wizard.</summary>
public sealed class RegistrationSession
    : AggregateRoot<RegistrationSessionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private RegistrationSession() { }

    private RegistrationSession(RegistrationSessionId id)
        : base(id) { }

    public RegistrationInstanceId InstanceId { get; private set; }
    public string SessionToken { get; private set; } = string.Empty;
    public int RequestedSeatCount { get; private set; }
    public int HeldConfirmedSeats { get; private set; }
    public int HeldWaitlistSeats { get; private set; }
    public RegistrationSessionStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public RegistrationId? RegistrationId { get; private set; }

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

    public static Result<RegistrationSession> CreateHold(
        RegistrationInstanceId instanceId,
        int requestedSeatCount,
        int heldConfirmedSeats,
        int heldWaitlistSeats,
        int ttlMinutes,
        DateTimeOffset nowUtc
    )
    {
        if (requestedSeatCount <= 0 || heldConfirmedSeats < 0 || heldWaitlistSeats < 0)
        {
            return EventsRegistrationErrors.SessionHoldInvalid;
        }

        var id = RegistrationSessionId.Create();
        var token =
            $"hold_{id.Value.ToString("N")[^16..]}{Guid.CreateVersion7().ToString("N")[^8..]}";
        return new RegistrationSession(id)
        {
            InstanceId = instanceId,
            SessionToken = token,
            RequestedSeatCount = requestedSeatCount,
            HeldConfirmedSeats = heldConfirmedSeats,
            HeldWaitlistSeats = heldWaitlistSeats,
            Status = RegistrationSessionStatus.Active,
            ExpiresAtUtc = nowUtc.ToUniversalTime().AddMinutes(Math.Clamp(ttlMinutes, 1, 120)),
        };
    }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return Status == RegistrationSessionStatus.Active && ExpiresAtUtc > nowUtc;
    }

    public void MarkCompleted(RegistrationId registrationId)
    {
        Status = RegistrationSessionStatus.Completed;
        RegistrationId = registrationId;
    }

    public bool MarkExpiredIfPastDue(DateTimeOffset nowUtc)
    {
        if (Status != RegistrationSessionStatus.Active || ExpiresAtUtc > nowUtc)
        {
            return false;
        }

        Status = RegistrationSessionStatus.Expired;
        return true;
    }
}
