using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal sealed class GetAttendanceMetricsSummaryQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetAttendanceMetricsSummaryQuery, AttendanceMetricsSummaryResponse>
{
    public async Task<Result<AttendanceMetricsSummaryResponse>> HandleAsync(
        GetAttendanceMetricsSummaryQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.EndDate < query.StartDate)
        {
            return GroupsErrors.InvalidDateRange;
        }

        var weekendTypeFlags = await dbContext
            .GroupTypes.AsNoTracking()
            .ToDictionaryAsync(
                static gt => gt.Id,
                static gt => gt.AttendanceCountsAsWeekendService,
                cancellationToken
            );
        var groupCampusMap = await dbContext
            .Groups.AsNoTracking()
            .ToDictionaryAsync(
                static g => g.Id,
                static g => (g.CampusId, g.GroupTypeId),
                cancellationToken
            );
        var occurrences = await LoadOccurrencesAsync(query, cancellationToken);

        return Result.Success(
            AggregateMetrics(query, occurrences, groupCampusMap, weekendTypeFlags)
        );
    }

    private async Task<List<AttendanceOccurrence>> LoadOccurrencesAsync(
        GetAttendanceMetricsSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var occQuery = dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Include(static o => o.Attendances)
            .Where(o => o.OccurrenceDate >= query.StartDate && o.OccurrenceDate <= query.EndDate);

        if (query.GroupId.HasValue)
        {
            var gid = GroupId.From(query.GroupId.Value);
            occQuery = occQuery.Where(o => o.GroupId == gid);
        }

        if (query.GroupTypeId.HasValue)
        {
            var gtid = GroupTypeId.From(query.GroupTypeId.Value);
            occQuery = occQuery.Where(o => o.RootGroupTypeId == gtid);
        }

        return await occQuery.ToListAsync(cancellationToken);
    }

    private static AttendanceMetricsSummaryResponse AggregateMetrics(
        GetAttendanceMetricsSummaryQuery query,
        List<AttendanceOccurrence> occurrences,
        Dictionary<GroupId, (Guid? CampusId, GroupTypeId GroupTypeId)> groupCampusMap,
        Dictionary<GroupTypeId, bool> weekendTypeFlags
    )
    {
        var state = new MetricsState();
        foreach (var occ in occurrences)
        {
            AccumulateOccurrence(occ, query, groupCampusMap, weekendTypeFlags, state);
        }

        return state.ToResponse(query.StartDate, query.EndDate);
    }

    private static void AccumulateOccurrence(
        AttendanceOccurrence occ,
        GetAttendanceMetricsSummaryQuery query,
        Dictionary<GroupId, (Guid? CampusId, GroupTypeId GroupTypeId)> groupCampusMap,
        Dictionary<GroupTypeId, bool> weekendTypeFlags,
        MetricsState state
    )
    {
        var (campusId, groupTypeId) = ResolveCampusAndGroupType(occ, groupCampusMap);
        var isWeekend =
            groupTypeId.HasValue && weekendTypeFlags.GetValueOrDefault(groupTypeId.Value);

        if (
            (query.WeekendServicesOnly && !isWeekend)
            || (query.CampusId.HasValue && campusId != query.CampusId.Value)
        )
        {
            return;
        }

        var bucket = state.GetOrAddBucket(campusId, occ.SundayDate, groupTypeId?.Value, isWeekend);
        bucket.OccurrenceCount++;
        if (occ.DidNotOccur == true)
        {
            bucket.DidNotOccurCount++;
            return;
        }

        var anon = occ.AnonymousAttendanceCount ?? 0;
        bucket.AnonymousCount += anon;
        state.TotalAnon += anon;

        var knownInOcc = AccumulateKnownAttendances(occ.Attendances, bucket, state);
        state.TotalKnown += knownInOcc;
        if (isWeekend)
        {
            state.WeekendTotal += knownInOcc + anon;
        }
    }

    private static (Guid? CampusId, GroupTypeId? GroupTypeId) ResolveCampusAndGroupType(
        AttendanceOccurrence occ,
        Dictionary<GroupId, (Guid? CampusId, GroupTypeId GroupTypeId)> groupCampusMap
    )
    {
        Guid? campusId = null;
        var groupTypeId = occ.RootGroupTypeId;
        if (occ.GroupId.HasValue && groupCampusMap.TryGetValue(occ.GroupId.Value, out var info))
        {
            campusId = info.CampusId;
            groupTypeId ??= info.GroupTypeId;
        }

        campusId ??= occ.Attendances.FirstOrDefault(static a => a.CampusId.HasValue)?.CampusId;
        return (campusId, groupTypeId);
    }

    private static int AccumulateKnownAttendances(
        IReadOnlyCollection<Attendance> attendances,
        BucketAccumulator bucket,
        MetricsState state
    )
    {
        var known = 0;
        foreach (var attendance in attendances)
        {
            if (attendance.DidAttend != true)
            {
                continue;
            }

            known++;
            bucket.KnownCount++;
            if (attendance.PersonId.HasValue)
            {
                _ = bucket.UniquePeople.Add(attendance.PersonId.Value);
                _ = state.AllUniquePeople.Add(attendance.PersonId.Value);
            }

            if (attendance.IsFirstTime == true)
            {
                bucket.FirstTimeCount++;
                state.TotalFirstTime++;
            }
        }

        return known;
    }

    private sealed class MetricsState
    {
        private readonly Dictionary<
            (Guid? CampusId, DateOnly SundayDate, Guid? GroupTypeId),
            BucketAccumulator
        > _buckets = [];

        public HashSet<Guid> AllUniquePeople { get; } = [];
        public int TotalKnown { get; set; }
        public int TotalAnon { get; set; }
        public int TotalFirstTime { get; set; }
        public int WeekendTotal { get; set; }

        public BucketAccumulator GetOrAddBucket(
            Guid? campusId,
            DateOnly sundayDate,
            Guid? groupTypeId,
            bool isWeekend
        )
        {
            var key = (campusId, sundayDate, groupTypeId);
            if (!_buckets.TryGetValue(key, out var acc))
            {
                acc = new BucketAccumulator(isWeekend);
                _buckets[key] = acc;
            }

            return acc;
        }

        public AttendanceMetricsSummaryResponse ToResponse(DateOnly startDate, DateOnly endDate)
        {
            AttendanceMetricBucketResponse[] buckets =
            [
                .. _buckets
                    .OrderBy(static kv => kv.Key.SundayDate)
                    .ThenBy(static kv => kv.Key.CampusId)
                    .Select(static kv => new AttendanceMetricBucketResponse(
                        kv.Key.CampusId,
                        kv.Key.SundayDate,
                        kv.Key.GroupTypeId,
                        kv.Value.OccurrenceCount,
                        kv.Value.DidNotOccurCount,
                        kv.Value.KnownCount,
                        kv.Value.AnonymousCount,
                        kv.Value.KnownCount + kv.Value.AnonymousCount,
                        kv.Value.UniquePeople.Count,
                        kv.Value.FirstTimeCount,
                        kv.Value.CountsAsWeekendService
                    )),
            ];

            return new AttendanceMetricsSummaryResponse(
                startDate,
                endDate,
                TotalKnown,
                TotalAnon,
                TotalKnown + TotalAnon,
                AllUniquePeople.Count,
                TotalFirstTime,
                WeekendTotal,
                buckets
            );
        }
    }

    private sealed class BucketAccumulator(bool countsAsWeekendService)
    {
        public bool CountsAsWeekendService { get; } = countsAsWeekendService;
        public int OccurrenceCount { get; set; }
        public int DidNotOccurCount { get; set; }
        public int KnownCount { get; set; }
        public int AnonymousCount { get; set; }
        public int FirstTimeCount { get; set; }
        public HashSet<Guid> UniquePeople { get; } = [];
    }
}
