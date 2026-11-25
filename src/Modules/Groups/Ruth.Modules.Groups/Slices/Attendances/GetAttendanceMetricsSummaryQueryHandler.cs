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

        var headlineTypeFlags = await dbContext
            .GroupTypes.AsNoTracking()
            .ToDictionaryAsync(
                static gt => gt.Id,
                static gt => gt.CountsTowardHeadlineAttendance,
                cancellationToken
            );
        var groupBranchMap = await dbContext
            .Groups.AsNoTracking()
            .ToDictionaryAsync(
                static g => g.Id,
                static g => (g.BranchId, g.GroupTypeId),
                cancellationToken
            );
        var occurrences = await LoadOccurrencesAsync(query, cancellationToken);

        return Result.Success(
            AggregateMetrics(query, occurrences, groupBranchMap, headlineTypeFlags)
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
        Dictionary<GroupId, (Guid? BranchId, GroupTypeId GroupTypeId)> groupBranchMap,
        Dictionary<GroupTypeId, bool> headlineTypeFlags
    )
    {
        var state = new MetricsState();
        foreach (var occ in occurrences)
        {
            AccumulateOccurrence(occ, query, groupBranchMap, headlineTypeFlags, state);
        }

        return state.ToResponse(query.StartDate, query.EndDate);
    }

    private static void AccumulateOccurrence(
        AttendanceOccurrence occ,
        GetAttendanceMetricsSummaryQuery query,
        Dictionary<GroupId, (Guid? BranchId, GroupTypeId GroupTypeId)> groupBranchMap,
        Dictionary<GroupTypeId, bool> headlineTypeFlags,
        MetricsState state
    )
    {
        var (branchId, groupTypeId) = ResolveBranchAndGroupType(occ, groupBranchMap);
        var isHeadline =
            groupTypeId.HasValue && headlineTypeFlags.GetValueOrDefault(groupTypeId.Value);

        if (
            (query.HeadlineAttendanceOnly && !isHeadline)
            || (query.BranchId.HasValue && branchId != query.BranchId.Value)
        )
        {
            return;
        }

        var bucket = state.GetOrAddBucket(
            branchId,
            occ.WeekEndDate,
            groupTypeId?.Value,
            isHeadline
        );
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
        if (isHeadline)
        {
            state.HeadlineTotal += knownInOcc + anon;
        }
    }

    private static (Guid? BranchId, GroupTypeId? GroupTypeId) ResolveBranchAndGroupType(
        AttendanceOccurrence occ,
        Dictionary<GroupId, (Guid? BranchId, GroupTypeId GroupTypeId)> groupBranchMap
    )
    {
        Guid? branchId = null;
        var groupTypeId = occ.RootGroupTypeId;
        if (occ.GroupId.HasValue && groupBranchMap.TryGetValue(occ.GroupId.Value, out var info))
        {
            branchId = info.BranchId;
            groupTypeId ??= info.GroupTypeId;
        }

        branchId ??= occ.Attendances.FirstOrDefault(static a => a.BranchId.HasValue)?.BranchId;
        return (branchId, groupTypeId);
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
            (Guid? BranchId, DateOnly WeekEndDate, Guid? GroupTypeId),
            BucketAccumulator
        > _buckets = [];

        public HashSet<Guid> AllUniquePeople { get; } = [];
        public int TotalKnown { get; set; }
        public int TotalAnon { get; set; }
        public int TotalFirstTime { get; set; }
        public int HeadlineTotal { get; set; }

        public BucketAccumulator GetOrAddBucket(
            Guid? branchId,
            DateOnly weekEndDate,
            Guid? groupTypeId,
            bool countsTowardHeadline
        )
        {
            var key = (branchId, weekEndDate, groupTypeId);
            if (!_buckets.TryGetValue(key, out var acc))
            {
                acc = new BucketAccumulator(countsTowardHeadline);
                _buckets[key] = acc;
            }

            return acc;
        }

        public AttendanceMetricsSummaryResponse ToResponse(DateOnly startDate, DateOnly endDate)
        {
            AttendanceMetricBucketResponse[] buckets =
            [
                .. _buckets
                    .OrderBy(static kv => kv.Key.WeekEndDate)
                    .ThenBy(static kv => kv.Key.BranchId)
                    .Select(static kv => new AttendanceMetricBucketResponse(
                        kv.Key.BranchId,
                        kv.Key.WeekEndDate,
                        kv.Key.GroupTypeId,
                        kv.Value.OccurrenceCount,
                        kv.Value.DidNotOccurCount,
                        kv.Value.KnownCount,
                        kv.Value.AnonymousCount,
                        kv.Value.KnownCount + kv.Value.AnonymousCount,
                        kv.Value.UniquePeople.Count,
                        kv.Value.FirstTimeCount,
                        kv.Value.CountsTowardHeadlineAttendance
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
                HeadlineTotal,
                buckets
            );
        }
    }

    private sealed class BucketAccumulator(bool countsTowardHeadlineAttendance)
    {
        public bool CountsTowardHeadlineAttendance { get; } = countsTowardHeadlineAttendance;
        public int OccurrenceCount { get; set; }
        public int DidNotOccurCount { get; set; }
        public int KnownCount { get; set; }
        public int AnonymousCount { get; set; }
        public int FirstTimeCount { get; set; }
        public HashSet<Guid> UniquePeople { get; } = [];
    }
}
