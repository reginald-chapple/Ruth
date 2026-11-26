using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Materialized membership row linking a Person to a <see cref="PersonalizationSegment" />.</summary>
public sealed class PersonalizationSegmentMembership : ITenantScoped
{
    private PersonalizationSegmentMembership() { }

    public PersonalizationSegmentMembership(
        PersonalizationSegmentId segmentId,
        Guid personId,
        DateTimeOffset addedDateTimeUtc,
        double affinityScore = 1.0
    )
    {
        SegmentId = segmentId;
        PersonId = personId;
        AddedDateTimeUtc = addedDateTimeUtc.ToUniversalTime();
        AffinityScore = affinityScore;
    }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    public PersonalizationSegmentId SegmentId { get; private set; }

    public Guid PersonId { get; private set; }

    public DateTimeOffset AddedDateTimeUtc { get; private set; }

    public double AffinityScore { get; private set; } = 1.0;

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign PersonalizationSegmentMembership ({SegmentId}/{PersonId}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }

    public void RepointPerson(Guid survivingPersonId)
    {
        PersonId = survivingPersonId;
    }
}
