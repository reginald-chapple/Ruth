using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Reusable cadence template (Every Week, Every Other Week, etc.) assigned to serving volunteers.</summary>
public sealed class GroupMemberScheduleTemplate
    : AggregateRoot<GroupMemberScheduleTemplateId>,
        IAuditableEntity
{
    private GroupMemberScheduleTemplate() { }

    private GroupMemberScheduleTemplate(GroupMemberScheduleTemplateId id)
        : base(id) { }

    /// <summary>Gets the template display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional GroupType scope (null means usable by any GroupType).</summary>
    public GroupTypeId? GroupTypeId { get; private set; }

    /// <summary>Gets the referenced Core Schedule identifier.</summary>
    public Guid ScheduleId { get; private set; }

    /// <summary>Gets the human-readable cadence summary.</summary>
    public string RecurrenceDescription { get; private set; } = "Every Week";

    /// <summary>Gets the rotation cadence in weeks (>= 1).</summary>
    public int IntervalWeeks { get; private set; } = 1;

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

    /// <summary>Returns true if <paramref name="occurrenceDate" /> aligns with this template starting from <paramref name="anchorDate" />.</summary>
    public bool MatchesDate(DateOnly? anchorDate, DateOnly occurrenceDate)
    {
        if (!anchorDate.HasValue || IntervalWeeks <= 1)
        {
            return true;
        }

        if (occurrenceDate < anchorDate.Value)
        {
            return false;
        }

        var dayDelta = occurrenceDate.DayNumber - anchorDate.Value.DayNumber;
        var weekDelta = dayDelta / 7;
        return weekDelta % IntervalWeeks == 0;
    }

    internal static Result<GroupMemberScheduleTemplate> Create(
        string name,
        Guid scheduleId,
        GroupTypeId? groupTypeId,
        string? recurrenceDescription,
        int intervalWeeks
    )
    {
        var nameResult = GroupsTextPolicy.NormalizeRequired(name);
        var descResult = GroupsTextPolicy.NormalizeOptional(
            recurrenceDescription,
            GroupsTextPolicy.ShortLabelMaxLength,
            allowNewLines: false
        );
        var combined = Result.Combine(nameResult, descResult);
        return combined.IsFailure ? Result.Failure<GroupMemberScheduleTemplate>(combined.Errors)
            : intervalWeeks < 1 ? (Result<GroupMemberScheduleTemplate>)GroupsErrors.InvalidCapacity
            : Result.Success(
                new GroupMemberScheduleTemplate(GroupMemberScheduleTemplateId.Create())
                {
                    Name = nameResult.Value,
                    ScheduleId = scheduleId,
                    GroupTypeId = groupTypeId,
                    RecurrenceDescription = descResult.Value ?? "Every Week",
                    IntervalWeeks = intervalWeeks,
                }
            );
    }
}
