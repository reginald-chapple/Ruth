using Ruth.Modules.Groups.Domain.Groups;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Min/Desired/Max volunteer staffing thresholds for a GroupLocation + Schedule pairing.</summary>
public sealed class GroupLocationScheduleConfig
    : AggregateRoot<GroupLocationScheduleConfigId>,
        IAuditableEntity
{
    private GroupLocationScheduleConfig() { }

    private GroupLocationScheduleConfig(
        GroupLocationScheduleConfigId id,
        GroupLocationId groupLocationId,
        Guid scheduleId
    )
        : base(id)
    {
        GroupLocationId = groupLocationId;
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the owning GroupLocation identifier.</summary>
    public GroupLocationId GroupLocationId { get; private set; }

    /// <summary>Gets the referenced Core Schedule identifier.</summary>
    public Guid ScheduleId { get; private set; }

    /// <summary>Gets the optional display label for this slot configuration.</summary>
    public string? ConfigurationName { get; private set; }

    /// <summary>Gets the minimum required volunteer count.</summary>
    public int? MinimumCapacity { get; private set; }

    /// <summary>Gets the target desired volunteer count.</summary>
    public int? DesiredCapacity { get; private set; }

    /// <summary>Gets the maximum permitted volunteer count.</summary>
    public int? MaximumCapacity { get; private set; }

    /// <summary>Gets optional confirmation email/message instructions.</summary>
    public string? ConfirmationAdditionalDetails { get; private set; }

    /// <summary>Gets optional reminder email/message instructions.</summary>
    public string? ReminderAdditionalDetails { get; private set; }

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

    internal static Result<GroupLocationScheduleConfig> Create(
        GroupLocationId groupLocationId,
        Guid scheduleId,
        string? configurationName,
        int? minimumCapacity,
        int? desiredCapacity,
        int? maximumCapacity,
        string? confirmationDetails,
        string? reminderDetails
    )
    {
        var config = new GroupLocationScheduleConfig(
            GroupLocationScheduleConfigId.Create(),
            groupLocationId,
            scheduleId
        );
        var update = config.Update(
            configurationName,
            minimumCapacity,
            desiredCapacity,
            maximumCapacity,
            confirmationDetails,
            reminderDetails
        );
        return update.IsFailure
            ? Result.Failure<GroupLocationScheduleConfig>(update.Errors)
            : Result.Success(config);
    }

    internal Result Update(
        string? configurationName,
        int? minimumCapacity,
        int? desiredCapacity,
        int? maximumCapacity,
        string? confirmationDetails,
        string? reminderDetails
    )
    {
        if (!AreCapacitiesOrdered(minimumCapacity, desiredCapacity, maximumCapacity))
        {
            return GroupsErrors.InvalidScheduleCapacities;
        }

        var nameResult = GroupsTextPolicy.NormalizeOptional(
            configurationName,
            GroupsTextPolicy.NameMaxLength,
            allowNewLines: false
        );
        var confirmResult = GroupsTextPolicy.NormalizeOptional(confirmationDetails);
        var reminderResult = GroupsTextPolicy.NormalizeOptional(reminderDetails);
        var combined = Result.Combine(nameResult, confirmResult, reminderResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        ConfigurationName = nameResult.Value;
        MinimumCapacity = minimumCapacity;
        DesiredCapacity = desiredCapacity;
        MaximumCapacity = maximumCapacity;
        ConfirmationAdditionalDetails = confirmResult.Value;
        ReminderAdditionalDetails = reminderResult.Value;
        return Result.Success();
    }

    private static bool AreCapacitiesOrdered(int? min, int? desired, int? max)
    {
        return min is not < 0
            && desired is not < 0
            && max is not < 0
            && (!min.HasValue || !desired.HasValue || min.Value <= desired.Value)
            && (!desired.HasValue || !max.HasValue || desired.Value <= max.Value)
            && (!min.HasValue || !max.HasValue || min.Value <= max.Value);
    }
}
