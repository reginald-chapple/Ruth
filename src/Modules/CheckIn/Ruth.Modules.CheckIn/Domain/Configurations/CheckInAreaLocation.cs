using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>Represents a physical room/location and its active schedules and capacity thresholds inside a <see cref="CheckInArea" />.</summary>
public sealed class CheckInAreaLocation : Entity<CheckInAreaLocationId>
{
    private CheckInAreaLocation() { }

    private CheckInAreaLocation(
        CheckInAreaLocationId id,
        CheckInAreaId checkInAreaId,
        Guid locationId
    )
        : base(id)
    {
        CheckInAreaId = checkInAreaId;
        LocationId = locationId;
    }

    /// <summary>Gets the owning check-in area identifier.</summary>
    public CheckInAreaId CheckInAreaId { get; private set; }

    /// <summary>Gets the Core module Location identifier.</summary>
    public Guid LocationId { get; private set; }

    /// <summary>Gets the room/location display label.</summary>
    public string LocationName { get; private set; } = string.Empty;

    /// <summary>Gets the distinct Core Schedule identifiers enabled for this room.</summary>
    public IReadOnlyList<Guid> ScheduleIds { get; private set; } = [];

    /// <summary>Gets the optional soft capacity warning/overflow threshold.</summary>
    public int? SoftCapacity { get; private set; }

    /// <summary>Gets the optional firm capacity hard ceiling.</summary>
    public int? FirmCapacity { get; private set; }

    /// <summary>Gets the ascending fill/tie-break priority order.</summary>
    public int PriorityOrder { get; private set; }

    /// <summary>Gets whether the room is currently open for check-in.</summary>
    public bool IsOpenForCheckIn { get; private set; } = true;

    /// <summary>Gets whether the room is held back as an overflow destination until primary rooms reach soft capacity.</summary>
    public bool IsOverflowRoom { get; private set; }

    internal static Result<CheckInAreaLocation> Create(
        CheckInAreaId areaId,
        CheckInRoomMatrixCellInputDto input,
        CheckInAreaLocationId? existingId = null
    )
    {
        if (input.LocationId == Guid.Empty)
        {
            return Result.Failure<CheckInAreaLocation>(CheckInErrors.LocationRequired);
        }

        var cell = new CheckInAreaLocation(
            existingId ?? CheckInAreaLocationId.Create(),
            areaId,
            input.LocationId
        );
        var applied = cell.Apply(input);
        return applied.IsFailure
            ? Result.Failure<CheckInAreaLocation>(applied.Errors)
            : Result.Success(cell);
    }

    internal Result SetAvailability(bool isOpenForCheckIn, int? softCapacity, int? firmCapacity)
    {
        if (!AreValidCapacities(softCapacity, firmCapacity))
        {
            return CheckInErrors.InvalidRoomCapacities;
        }

        IsOpenForCheckIn = isOpenForCheckIn;
        SoftCapacity = softCapacity;
        FirmCapacity = firmCapacity;
        return Result.Success();
    }

    internal bool SupportsSchedule(Guid scheduleId)
    {
        return ScheduleIds.Contains(scheduleId);
    }

    internal Result Apply(CheckInRoomMatrixCellInputDto input)
    {
        var nameResult = CheckInTextPolicy.NormalizeRequired(
            input.LocationName,
            CheckInTextPolicy.NameMaxLength,
            CheckInErrors.LocationNameRequired,
            CheckInErrors.LocationNameTooLong
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Errors);
        }

        if (!AreValidCapacities(input.SoftCapacity, input.FirmCapacity))
        {
            return CheckInErrors.InvalidRoomCapacities;
        }

        LocationName = nameResult.Value;
        ScheduleIds = input.ScheduleIds is null
            ? []
            : [.. input.ScheduleIds.Where(static id => id != Guid.Empty).Distinct()];
        SoftCapacity = input.SoftCapacity;
        FirmCapacity = input.FirmCapacity;
        PriorityOrder = input.PriorityOrder;
        IsOpenForCheckIn = input.IsOpenForCheckIn;
        IsOverflowRoom = input.IsOverflowRoom;
        return Result.Success();
    }

    private static bool AreValidCapacities(int? softCapacity, int? firmCapacity)
    {
        var softOk = !softCapacity.HasValue || softCapacity.Value >= 0;
        var firmOk = !firmCapacity.HasValue || firmCapacity.Value >= 0;
        var orderOk =
            !softCapacity.HasValue
            || !firmCapacity.HasValue
            || softCapacity.Value <= firmCapacity.Value;
        return softOk && firmOk && orderOk;
    }
}
