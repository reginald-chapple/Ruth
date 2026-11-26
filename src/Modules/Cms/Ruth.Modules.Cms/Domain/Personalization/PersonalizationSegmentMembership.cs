namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Materialized membership row linking a Person to a <see cref="PersonalizationSegment" />.</summary>
public sealed class PersonalizationSegmentMembership
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

    public PersonalizationSegmentId SegmentId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateTimeOffset AddedDateTimeUtc { get; private set; }
    public double AffinityScore { get; private set; } = 1.0;

    public void RepointPerson(Guid survivingPersonId)
    {
        PersonId = survivingPersonId;
    }
}
