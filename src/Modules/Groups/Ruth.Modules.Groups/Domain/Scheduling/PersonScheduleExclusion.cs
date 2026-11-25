using Ruth.Modules.Groups.Domain.Groups;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Personal (or family-linked) blackout date range indicating volunteer unavailability.</summary>
public sealed class PersonScheduleExclusion
    : AggregateRoot<PersonScheduleExclusionId>,
        IAuditableEntity
{
    private PersonScheduleExclusion() { }

    private PersonScheduleExclusion(PersonScheduleExclusionId id, Guid personId)
        : base(id)
    {
        PersonId = personId;
    }

    /// <summary>Gets the CRM Person identifier.</summary>
    public Guid PersonId { get; private set; }

    /// <summary>Gets the optional Group scope (null applies across all serving groups).</summary>
    public GroupId? GroupId { get; private set; }

    /// <summary>Gets the optional parent exclusion identifier for family-linked blackouts.</summary>
    public PersonScheduleExclusionId? ParentExclusionId { get; private set; }

    /// <summary>Gets the optional title (e.g., "Summer Vacation").</summary>
    public string? Title { get; private set; }

    /// <summary>Gets the inclusive start date.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Gets the inclusive end date.</summary>
    public DateOnly EndDate { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <summary>Returns true if this blackout covers <paramref name="date" /> for <paramref name="groupId" />.</summary>
    public bool Covers(DateOnly date, GroupId groupId)
    {
        return date >= StartDate
            && date <= EndDate
            && (!GroupId.HasValue || GroupId.Value == groupId);
    }

    internal static Result<PersonScheduleExclusion> Create(
        Guid personId,
        DateOnly startDate,
        DateOnly endDate,
        string? title,
        GroupId? groupId,
        PersonScheduleExclusionId? parentExclusionId
    )
    {
        if (endDate < startDate)
        {
            return GroupsErrors.InvalidDateRange;
        }

        var titleResult = GroupsTextPolicy.NormalizeOptional(
            title,
            GroupsTextPolicy.NameMaxLength,
            allowNewLines: false
        );
        return titleResult.IsFailure
            ? Result.Failure<PersonScheduleExclusion>(titleResult.Errors)
            : Result.Success(
                new PersonScheduleExclusion(PersonScheduleExclusionId.Create(), personId)
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Title = titleResult.Value,
                    GroupId = groupId,
                    ParentExclusionId = parentExclusionId,
                }
            );
    }

    internal void RepointPerson(Guid survivingPersonId)
    {
        PersonId = survivingPersonId;
    }
}
