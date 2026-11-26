using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Audience segment combining interaction affinity, request filters, and DataView membership.</summary>
public sealed class PersonalizationSegment
    : AggregateRoot<PersonalizationSegmentId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Cms.PersonalizationSegment";

    private PersonalizationSegment() { }

    private PersonalizationSegment(PersonalizationSegmentId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string SegmentKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int TimeToUpdateIntervalMinutes { get; private set; } = 60;
    public bool IsActive { get; private set; } = true;
    public bool IsDirty { get; private set; } = true;
    public PersonalizationSegmentFilterSpec FilterSpec { get; private set; } = new();
    public DateTimeOffset? LastEvaluatedUtc { get; private set; }
    public int CachedMemberCount { get; private set; }

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

    public static Result<PersonalizationSegment> Create(
        string name,
        string segmentKey,
        string? description = null,
        int timeToUpdateIntervalMinutes = 60,
        PersonalizationSegmentFilterSpec? filterSpec = null
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(segmentKey);
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedKey)
            ? Result.Failure<PersonalizationSegment>(
                CmsErrors.Validation(
                    "PersonalizationSegment.Invalid",
                    "Segment name and key are required."
                )
            )
            : Result.Success(
                new PersonalizationSegment(PersonalizationSegmentId.Create())
                {
                    Name = name.Trim(),
                    SegmentKey = normalizedKey,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    TimeToUpdateIntervalMinutes = Math.Clamp(timeToUpdateIntervalMinutes, 1, 10080),
                    FilterSpec = filterSpec ?? new PersonalizationSegmentFilterSpec(),
                    IsActive = true,
                    IsDirty = true,
                }
            );
    }

    public void RecordEvaluation(int memberCount, DateTimeOffset evaluatedAtUtc)
    {
        CachedMemberCount = Math.Max(0, memberCount);
        LastEvaluatedUtc = evaluatedAtUtc.ToUniversalTime();
        IsDirty = false;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }
}
