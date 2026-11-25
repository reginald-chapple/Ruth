using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/groups/attendance")
            .WithTags("Group Attendance")
            .RequireAuthorization();

        _ = group
            .MapPost("/groups/{groupId:guid}/record", RecordAsync)
            .WithName("GroupsRecordAttendance")
            .WithSummary("Record or update a Group AttendanceOccurrence and roster checklist")
            .Produces<AttendanceOccurrenceDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/groups/{groupId:guid}/occurrences", ListOccurrencesAsync)
            .WithName("GroupsListAttendanceOccurrences")
            .WithSummary("List AttendanceOccurrences for a Group")
            .Produces<IReadOnlyList<AttendanceOccurrenceSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/occurrences/{occurrenceId:guid}", GetOccurrenceByIdAsync)
            .WithName("GroupsGetAttendanceOccurrenceById")
            .WithSummary("Get AttendanceOccurrence detail including all roster attendance entries")
            .Produces<AttendanceOccurrenceDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/metrics", GetMetricsAsync)
            .WithName("GroupsGetAttendanceMetricsSummary")
            .WithSummary("Compute Campus and Weekend Service attendance metric summaries")
            .Produces<AttendanceMetricsSummaryResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<AttendanceOccurrenceDetailResponse>, ProblemHttpResult>
    > RecordAsync(
        Guid groupId,
        RecordGroupAttendanceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse
        >(command with { GroupId = groupId }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AttendanceOccurrenceSummaryResponse>>, ProblemHttpResult>
    > ListOccurrencesAsync(
        Guid groupId,
        DateOnly? startDate,
        DateOnly? endDate,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetGroupAttendanceOccurrencesQuery,
            IReadOnlyList<AttendanceOccurrenceSummaryResponse>
        >(
            new GetGroupAttendanceOccurrencesQuery(groupId, startDate, endDate, maxResults ?? 50),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttendanceOccurrenceDetailResponse>, ProblemHttpResult>
    > GetOccurrenceByIdAsync(
        Guid occurrenceId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttendanceOccurrenceByIdQuery,
            AttendanceOccurrenceDetailResponse
        >(new GetAttendanceOccurrenceByIdQuery(occurrenceId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttendanceMetricsSummaryResponse>, ProblemHttpResult>
    > GetMetricsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? campusId,
        Guid? groupTypeId,
        Guid? groupId,
        bool? weekendServicesOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttendanceMetricsSummaryQuery,
            AttendanceMetricsSummaryResponse
        >(
            new GetAttendanceMetricsSummaryQuery(
                startDate,
                endDate,
                campusId,
                groupTypeId,
                groupId,
                weekendServicesOnly ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : GroupsProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
