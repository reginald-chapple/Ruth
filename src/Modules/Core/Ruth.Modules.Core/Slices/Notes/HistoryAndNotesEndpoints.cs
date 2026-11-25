using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Notes;

internal static class HistoryAndNotesEndpoints
{
    public static IEndpointRouteBuilder MapHistoryAndNotesEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var history = endpoints
            .MapGroup("/api/core/history")
            .WithTags("History Log")
            .RequireAuthorization();
        _ = history
            .MapGet("/", GetHistoryAsync)
            .WithName("CoreGetHistoryLog")
            .WithSummary("Query structured domain history timeline")
            .Produces<IReadOnlyList<HistoryLogEntryResponse>>(StatusCodes.Status200OK);
        _ = history
            .MapPost("/", RecordHistoryAsync)
            .WithName("CoreRecordHistoryLog")
            .WithSummary("Record a structured domain change entry")
            .Produces<HistoryLogEntryResponse>(StatusCodes.Status201Created);

        var notes = endpoints.MapGroup("/api/core/notes").WithTags("Notes").RequireAuthorization();
        MapNoteRoutes(notes);

        return endpoints;
    }

    private static void MapNoteRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/types", GetNoteTypesAsync)
            .WithName("CoreGetNoteTypes")
            .WithSummary("List configured note types")
            .Produces<IReadOnlyList<NoteTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/types", CreateNoteTypeAsync)
            .WithName("CoreCreateNoteType")
            .WithSummary("Create a note type")
            .Produces<NoteTypeResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/timeline/{entityId:guid}", GetTimelineAsync)
            .WithName("CoreGetNotesTimeline")
            .WithSummary("Get threaded notes timeline for an entity")
            .Produces<IReadOnlyList<NoteResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateNoteAsync)
            .WithName("CoreCreateNote")
            .WithSummary("Create a root note or threaded reply")
            .Produces<NoteResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPut("/{id:guid}/approval", SetApprovalAsync)
            .WithName("CoreSetNoteApproval")
            .WithSummary("Approve or deny a pending note")
            .Produces<NoteResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}", DeleteNoteAsync)
            .WithName("CoreDeleteNote")
            .WithSummary("Delete a note")
            .Produces(StatusCodes.Status204NoContent);

        _ = group
            .MapPost("/watches", SetWatchAsync)
            .WithName("CoreSetNoteWatch")
            .WithSummary("Create or update a note watch subscription")
            .Produces<NoteWatchResponse>(StatusCodes.Status201Created);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<HistoryLogEntryResponse>>, ProblemHttpResult>
    > GetHistoryAsync(
        Guid? entityTypeId,
        Guid? entityId,
        Guid? categoryId,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetHistoryLogQuery,
            IReadOnlyList<HistoryLogEntryResponse>
        >(
            new GetHistoryLogQuery(entityTypeId, entityId, categoryId, maxResults ?? 100),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<HistoryLogEntryResponse>, ProblemHttpResult>
    > RecordHistoryAsync(
        RecordHistoryLogCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<RecordHistoryLogCommand, HistoryLogEntryResponse>(
            command with
            {
                ChangedByUserId = command.ChangedByUserId ?? userId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/history/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<NoteTypeResponse>>, ProblemHttpResult>
    > GetNoteTypesAsync(
        Guid? entityTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetNoteTypesQuery,
            IReadOnlyList<NoteTypeResponse>
        >(new GetNoteTypesQuery(entityTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<NoteTypeResponse>, ProblemHttpResult>
    > CreateNoteTypeAsync(
        CreateNoteTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateNoteTypeCommand, NoteTypeResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/notes/types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<NoteResponse>>, ProblemHttpResult>
    > GetTimelineAsync(
        Guid entityId,
        Guid? noteTypeId,
        bool? includeUnapproved,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var viewerId = TryResolveUserId(httpContext);
        var result = await dispatcher.QueryAsync<
            GetNotesTimelineQuery,
            IReadOnlyList<NoteResponse>
        >(
            new GetNotesTimelineQuery(entityId, noteTypeId, viewerId, includeUnapproved ?? false),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<NoteResponse>, ProblemHttpResult>> CreateNoteAsync(
        CreateNoteCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<CreateNoteCommand, NoteResponse>(
            command with
            {
                AuthorUserId = command.AuthorUserId ?? userId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/notes/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<NoteResponse>, ProblemHttpResult>> SetApprovalAsync(
        Guid id,
        SetNoteApprovalCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<SetNoteApprovalCommand, NoteResponse>(
            command with
            {
                NoteId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteNoteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var isAdmin = httpContext.User.IsInRole("Administrators");
        var result = await dispatcher.SendAsync<DeleteNoteCommand, bool>(
            new DeleteNoteCommand(id, userId, isAdmin),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<NoteWatchResponse>, ProblemHttpResult>> SetWatchAsync(
        SetNoteWatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<SetNoteWatchCommand, NoteWatchResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/notes/watches/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static Guid? TryResolveUserId(HttpContext context)
    {
        return Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? parsed
            : null;
    }
}
