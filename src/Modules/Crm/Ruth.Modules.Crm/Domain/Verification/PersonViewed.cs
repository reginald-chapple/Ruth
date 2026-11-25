using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>Audit log entry capturing when a staff member viewed a target person profile.</summary>
public sealed class PersonViewed : Entity<PersonViewedId>
{
    private PersonViewed() { }

    private PersonViewed(
        PersonViewedId id,
        PersonId targetPersonId,
        PersonId? viewerPersonId,
        Guid? viewerUserId,
        DateTimeOffset viewedOnUtc,
        string? ipAddress,
        string? source
    )
        : base(id)
    {
        TargetPersonId = targetPersonId;
        ViewerPersonId = viewerPersonId;
        ViewerUserId = viewerUserId;
        ViewedOnUtc = viewedOnUtc.ToUniversalTime();
        IpAddress = ipAddress;
        Source = source;
    }

    /// <summary>Gets the viewed target person identifier.</summary>
    public PersonId TargetPersonId { get; private set; }

    /// <summary>Gets the optional viewer person identifier.</summary>
    public PersonId? ViewerPersonId { get; private set; }

    /// <summary>Gets the optional viewer identity user identifier.</summary>
    public Guid? ViewerUserId { get; private set; }

    /// <summary>Gets the UTC timestamp when the profile was viewed.</summary>
    public DateTimeOffset ViewedOnUtc { get; private set; }

    /// <summary>Gets the optional client IP address.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Gets the optional application/site source name.</summary>
    public string? Source { get; private set; }

    /// <summary>Creates a new profile view audit record.</summary>
    public static PersonViewed Create(
        PersonId targetPersonId,
        DateTimeOffset viewedOnUtc,
        PersonId? viewerPersonId = null,
        Guid? viewerUserId = null,
        string? ipAddress = null,
        string? source = null
    )
    {
        return new PersonViewed(
            PersonViewedId.Create(),
            targetPersonId,
            viewerPersonId,
            viewerUserId,
            viewedOnUtc,
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            string.IsNullOrWhiteSpace(source) ? null : source.Trim()
        );
    }

    internal void RepointTarget(PersonId targetPersonId)
    {
        TargetPersonId = targetPersonId;
    }
}
