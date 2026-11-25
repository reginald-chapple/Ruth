using Ruth.Modules.CheckIn.Contracts.Configurations;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>
/// Pure domain room-balancing engine enforcing schedule activation, soft/firm capacity thresholds,
/// overflow room staging, and deterministic multi-strategy room ranking.
/// </summary>
public static class RoomBalancingDomainService
{
    private sealed record ScoredRoom(
        CheckInAreaLocation Room,
        int CurrentOccupancy,
        RoomThresholdStatus Status,
        decimal OccupancyRatio
    );

    /// <summary>Evaluates room states and selects the balanced recommended destination for <paramref name="scheduleId" />.</summary>
    public static (
        Guid? RecommendedLocationId,
        string? RecommendedLocationName,
        IReadOnlyList<EvaluatedCheckInRoomOptionResponse> CandidateRooms
    ) EvaluateAreaRooms(
        CheckInArea area,
        Guid scheduleId,
        IReadOnlyDictionary<Guid, int>? currentOccupancyByLocationId = null
    )
    {
        ArgumentNullException.ThrowIfNull(area);

        var scored = ScoreAllRooms(area.Rooms, scheduleId, currentOccupancyByLocationId);
        var activePool = SelectAssignablePool(scored);
        var rankedPool = OrderByStrategy(activePool, area.BalancingStrategy);
        var recommended = rankedPool.FirstOrDefault();

        var orderedForDisplay = BuildDisplayOrder(scored, area.BalancingStrategy);
        var responses = orderedForDisplay
            .Select(s => new EvaluatedCheckInRoomOptionResponse(
                s.Room.LocationId,
                s.Room.LocationName,
                s.CurrentOccupancy,
                s.Room.SoftCapacity,
                s.Room.FirmCapacity,
                s.Room.IsOverflowRoom,
                s.Status,
                s.OccupancyRatio,
                recommended is not null && recommended.Room.LocationId == s.Room.LocationId
            ))
            .ToList();

        return (recommended?.Room.LocationId, recommended?.Room.LocationName, responses);
    }

    /// <summary>Computes the threshold state for a single room cell given active schedule and headcount.</summary>
    public static RoomThresholdStatus EvaluateThresholdStatus(
        CheckInAreaLocation room,
        Guid scheduleId,
        int currentOccupancy
    )
    {
        ArgumentNullException.ThrowIfNull(room);

        return !room.IsOpenForCheckIn || !room.SupportsSchedule(scheduleId)
                ? RoomThresholdStatus.Closed
            : room.FirmCapacity is { } firm && currentOccupancy >= firm
                ? RoomThresholdStatus.AtFirmCapacity
            : room.SoftCapacity is { } soft && currentOccupancy >= soft
                ? RoomThresholdStatus.AtSoftCapacity
            : RoomThresholdStatus.Open;
    }

    /// <summary>Computes occupancy ratio against soft capacity (or firm capacity fallback).</summary>
    public static decimal ComputeOccupancyRatio(
        int currentOccupancy,
        int? softCapacity,
        int? firmCapacity
    )
    {
        var denominator = softCapacity ?? firmCapacity;
        if (!denominator.HasValue || denominator.Value <= 0)
        {
            return currentOccupancy <= 0 ? 0m : currentOccupancy;
        }

        var ratio = (decimal)Math.Max(0, currentOccupancy) / denominator.Value;
        return Math.Round(ratio, 4, MidpointRounding.AwayFromZero);
    }

    private static List<ScoredRoom> ScoreAllRooms(
        IReadOnlyCollection<CheckInAreaLocation> rooms,
        Guid scheduleId,
        IReadOnlyDictionary<Guid, int>? occupancyMap
    )
    {
        var list = new List<ScoredRoom>(rooms.Count);
        foreach (var room in rooms)
        {
            var count =
                occupancyMap is not null
                && occupancyMap.TryGetValue(room.LocationId, out var active)
                    ? Math.Max(0, active)
                    : 0;
            var status = EvaluateThresholdStatus(room, scheduleId, count);
            var ratio = ComputeOccupancyRatio(count, room.SoftCapacity, room.FirmCapacity);
            list.Add(new ScoredRoom(room, count, status, ratio));
        }

        return list;
    }

    private static List<ScoredRoom> SelectAssignablePool(List<ScoredRoom> scored)
    {
        // Tier 1: Open primary (non-overflow) rooms below soft capacity.
        var primaryOpen = scored
            .Where(static s => !s.Room.IsOverflowRoom && s.Status == RoomThresholdStatus.Open)
            .ToList();
        if (primaryOpen.Count > 0)
        {
            return primaryOpen;
        }

        // Tier 2: Open overflow rooms once all primary rooms reached soft/firm/closed.
        var overflowOpen = scored
            .Where(static s => s.Room.IsOverflowRoom && s.Status == RoomThresholdStatus.Open)
            .ToList();
        if (overflowOpen.Count > 0)
        {
            return overflowOpen;
        }

        // Tier 3: Rooms at/above soft capacity that have not yet hit firm capacity.
        return
        [
            .. scored.Where(static s =>
                s.Status == RoomThresholdStatus.AtSoftCapacity && !s.Room.IsOverflowRoom
            ),
            .. scored.Where(static s =>
                s.Status == RoomThresholdStatus.AtSoftCapacity && s.Room.IsOverflowRoom
            ),
        ];
    }

    private static List<ScoredRoom> OrderByStrategy(
        List<ScoredRoom> pool,
        RoomBalancingStrategy strategy
    )
    {
        return strategy switch
        {
            RoomBalancingStrategy.OrderedPriority =>
            [
                .. pool.OrderBy(static s => s.Room.PriorityOrder)
                    .ThenBy(static s => s.CurrentOccupancy)
                    .ThenBy(static s => s.Room.LocationId),
            ],
            RoomBalancingStrategy.LeastOccupancyCount =>
            [
                .. pool.OrderBy(static s => s.CurrentOccupancy)
                    .ThenBy(static s => s.Room.PriorityOrder)
                    .ThenBy(static s => s.Room.LocationId),
            ],
            RoomBalancingStrategy.LowestCapacityRatio =>
            [
                .. pool.OrderBy(static s => s.OccupancyRatio)
                    .ThenBy(static s => s.CurrentOccupancy)
                    .ThenBy(static s => s.Room.PriorityOrder)
                    .ThenBy(static s => s.Room.LocationId),
            ],
            _ =>
            [
                .. pool.OrderBy(static s => s.OccupancyRatio)
                    .ThenBy(static s => s.CurrentOccupancy)
                    .ThenBy(static s => s.Room.PriorityOrder)
                    .ThenBy(static s => s.Room.LocationId),
            ],
        };
    }

    private static List<ScoredRoom> BuildDisplayOrder(
        List<ScoredRoom> scored,
        RoomBalancingStrategy strategy
    )
    {
        return
        [
            .. OrderByStrategy(scored, strategy)
                .OrderBy(static s => (int)s.Status)
                .ThenBy(static s => s.Room.IsOverflowRoom ? 1 : 0),
        ];
    }
}
