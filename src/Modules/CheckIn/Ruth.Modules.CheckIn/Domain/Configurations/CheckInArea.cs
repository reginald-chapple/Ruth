using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>Check-in area owning participant eligibility rules and a room/location/schedule matrix.</summary>
public sealed class CheckInArea : Entity<CheckInAreaId>
{
    private readonly List<CheckInAreaLocation> _rooms = [];

    private CheckInArea() { }

    private CheckInArea(CheckInAreaId id, CheckInConfigurationId configurationId)
        : base(id)
    {
        CheckInConfigurationId = configurationId;
    }

    /// <summary>Gets the parent check-in configuration identifier.</summary>
    public CheckInConfigurationId CheckInConfigurationId { get; private set; }

    /// <summary>Gets the display name of the check-in area.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name used for area uniqueness.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional linked Groups module Group identifier.</summary>
    public Guid? LinkedGroupId { get; private set; }

    /// <summary>Gets the room-balancing strategy applied across open rooms.</summary>
    public RoomBalancingStrategy BalancingStrategy { get; private set; }

    /// <summary>Gets the display and evaluation order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether the area is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether check-ins into this area trigger badge printing.</summary>
    public bool PrintBadge { get; private set; } = true;

    /// <summary>Gets whether check-ins into this area issue a guardian pickup security code.</summary>
    public bool IssueSecurityPickupCode { get; private set; }

    /// <summary>Gets the validated participant eligibility specification.</summary>
    public CheckInEligibilityRule Eligibility { get; private set; } =
        CheckInEligibilityRule.Create(dto: null).Value;

    /// <summary>Gets the room/location/schedule matrix cells.</summary>
    public IReadOnlyCollection<CheckInAreaLocation> Rooms => _rooms.AsReadOnly();

    internal static Result<CheckInArea> Create(
        CheckInConfigurationId configurationId,
        CheckInAreaSpec spec,
        CheckInAreaId? areaId = null
    )
    {
        var area = new CheckInArea(areaId ?? CheckInAreaId.Create(), configurationId);
        var updated = area.Apply(spec);
        return updated.IsFailure
            ? Result.Failure<CheckInArea>(updated.Errors)
            : Result.Success(area);
    }

    internal Result Apply(CheckInAreaSpec spec)
    {
        var nameResult = CheckInTextPolicy.NormalizeRequired(
            spec.Name,
            CheckInTextPolicy.NameMaxLength,
            CheckInErrors.AreaNameRequired,
            CheckInErrors.AreaNameTooLong
        );
        var descResult = CheckInTextPolicy.NormalizeOptional(
            spec.Description,
            CheckInTextPolicy.DescriptionMaxLength
        );
        var eligibilityResult = CheckInEligibilityRule.Create(spec.Eligibility);
        var combined = Result.Combine(nameResult, descResult, eligibilityResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        var normalizedGroupId = NormalizeGroupId(spec.LinkedGroupId);
        var groupAndRoomsCheck = ValidateGroupAndRooms(
            eligibilityResult.Value.EnrollmentMode,
            normalizedGroupId,
            spec.Rooms
        );
        if (groupAndRoomsCheck.IsFailure)
        {
            return groupAndRoomsCheck;
        }

        ApplyValidatedScalars(
            spec,
            nameResult.Value,
            descResult.Value,
            normalizedGroupId,
            eligibilityResult.Value
        );
        if (spec.Rooms is not null)
        {
            SynchronizeRooms(spec.Rooms);
        }

        return Result.Success();
    }

    private static Guid? NormalizeGroupId(Guid? rawGroupId)
    {
        return rawGroupId is { } gid && gid != Guid.Empty ? gid : null;
    }

    private Result ValidateGroupAndRooms(
        EligibilityEnrollmentMode mode,
        Guid? normalizedGroupId,
        IReadOnlyList<CheckInRoomMatrixCellInputDto>? rooms
    )
    {
        return RequiresLinkedGroup(mode) && !normalizedGroupId.HasValue
                ? CheckInErrors.PreEnrolledRequiresLinkedGroup
            : rooms is null ? Result.Success()
            : ValidateRoomInputs(rooms);
    }

    private void ApplyValidatedScalars(
        CheckInAreaSpec spec,
        string normalizedName,
        string? description,
        Guid? normalizedGroupId,
        CheckInEligibilityRule eligibility
    )
    {
        Name = normalizedName;
        NormalizedName = normalizedName.ToUpperInvariant();
        Description = description;
        LinkedGroupId = normalizedGroupId;
        BalancingStrategy = spec.BalancingStrategy;
        Order = spec.Order;
        IsActive = spec.IsActive;
        PrintBadge = spec.PrintBadge;
        IssueSecurityPickupCode = spec.IssueSecurityPickupCode;
        Eligibility = eligibility;
    }

    internal Result SetRoomAvailability(
        Guid locationId,
        bool isOpenForCheckIn,
        int? softCapacity,
        int? firmCapacity
    )
    {
        var room = _rooms.Find(r => r.LocationId == locationId);
        return room is null
            ? CheckInErrors.RoomNotFound
            : room.SetAvailability(isOpenForCheckIn, softCapacity, firmCapacity);
    }

    private Result ValidateRoomInputs(IReadOnlyList<CheckInRoomMatrixCellInputDto> inputs)
    {
        var seenLocations = new HashSet<Guid>();
        foreach (var item in inputs)
        {
            if (item.LocationId != Guid.Empty && !seenLocations.Add(item.LocationId))
            {
                return CheckInErrors.DuplicateAreaLocation;
            }

            var probe = CheckInAreaLocation.Create(Id, item);
            if (probe.IsFailure)
            {
                return Result.Failure(probe.Errors);
            }
        }

        return Result.Success();
    }

    private void SynchronizeRooms(IReadOnlyList<CheckInRoomMatrixCellInputDto> inputs)
    {
        var incomingLocations = inputs.Select(static i => i.LocationId).ToHashSet();
        _ = _rooms.RemoveAll(existing => !incomingLocations.Contains(existing.LocationId));

        foreach (var item in inputs)
        {
            var existing = _rooms.Find(r => r.LocationId == item.LocationId);
            if (existing is not null)
            {
                _ = existing.Apply(item);
            }
            else
            {
                _rooms.Add(CheckInAreaLocation.Create(Id, item).Value);
            }
        }
    }

    private static bool RequiresLinkedGroup(EligibilityEnrollmentMode mode)
    {
        return mode
            is EligibilityEnrollmentMode.PreEnrolledMembersOnly
                or EligibilityEnrollmentMode.ScheduledShiftOnly;
    }
}
