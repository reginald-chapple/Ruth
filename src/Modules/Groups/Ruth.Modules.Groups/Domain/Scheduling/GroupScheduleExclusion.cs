using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>GroupType-level date range during which serving occurrences do not run (e.g. holidays).</summary>
public sealed class GroupScheduleExclusion
    : AggregateRoot<GroupScheduleExclusionId>,
        IAuditableEntity
{
    private GroupScheduleExclusion() { }

    private GroupScheduleExclusion(GroupScheduleExclusionId id, GroupTypeId groupTypeId)
        : base(id)
    {
        GroupTypeId = groupTypeId;
    }

    /// <summary>Gets the owning GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the optional title (e.g., "Christmas Break").</summary>
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

    /// <summary>Returns true if <paramref name="date" /> falls within this exclusion range.</summary>
    public bool Covers(DateOnly date)
    {
        return date >= StartDate && date <= EndDate;
    }

    internal static Result<GroupScheduleExclusion> Create(
        GroupTypeId groupTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string? title
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
            ? Result.Failure<GroupScheduleExclusion>(titleResult.Errors)
            : Result.Success(
                new GroupScheduleExclusion(GroupScheduleExclusionId.Create(), groupTypeId)
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Title = titleResult.Value,
                }
            );
    }
}
