using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal static class CheckInLabelEndpoints
{
    public static IEndpointRouteBuilder MapCheckInLabelEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        MapCrudEndpoints(endpoints);
        MapPreviewAndPrintEndpoints(endpoints);
        return endpoints;
    }

    private static void MapCrudEndpoints(IEndpointRouteBuilder endpoints)
    {
        var labels = endpoints
            .MapGroup("/api/checkin/labels")
            .WithTags("Check-In Labels & Zebra ZPL")
            .RequireAuthorization();

        _ = labels
            .MapGet("/", ListLabelsAsync)
            .WithName("CheckInListLabels")
            .WithSummary("List check-in badge and receipt label definitions")
            .WithDescription("Returns check-in thermal label definitions ordered by print order.")
            .Produces<IReadOnlyList<CheckInLabelResponse>>(StatusCodes.Status200OK);

        _ = labels
            .MapGet("/{id:guid}", GetLabelByIdAsync)
            .WithName("CheckInGetLabelById")
            .WithSummary("Get a check-in label definition by ID")
            .WithDescription("Returns a single check-in label layout and ZPL configuration.")
            .Produces<CheckInLabelResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = labels
            .MapPost("/", CreateLabelAsync)
            .WithName("CheckInCreateLabel")
            .WithSummary("Create a check-in label definition")
            .WithDescription("Creates a new structured JSON/ZPL check-in badge or receipt label.")
            .Produces<CheckInLabelResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = labels
            .MapPut("/{id:guid}", UpdateLabelAsync)
            .WithName("CheckInUpdateLabel")
            .WithSummary("Update a check-in label definition")
            .WithDescription("Updates layout elements, dimensions, or ZPL overrides on a label.")
            .Produces<CheckInLabelResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = labels
            .MapDelete("/{id:guid}", DeleteLabelAsync)
            .WithName("CheckInDeleteLabel")
            .WithSummary("Delete a non-system check-in label")
            .WithDescription(
                "Deletes a custom check-in label; built-in system labels are protected."
            )
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPreviewAndPrintEndpoints(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGroup("/api/checkin/labels")
            .WithTags("Check-In Labels & Zebra ZPL")
            .RequireAuthorization()
            .MapPost("/preview", PreviewLabelAsync)
            .WithName("CheckInPreviewLabel")
            .WithSummary("Compile and preview a check-in label as Zebra ZPL II and visual elements")
            .WithDescription(
                "Evaluates merge tokens and compiles the label layout into Zebra ZPL II and WYSIWYG elements."
            )
            .Produces<CheckInLabelPreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = endpoints
            .MapGroup("/api/checkin/sessions")
            .WithTags("Check-In Labels & Zebra ZPL")
            .RequireAuthorization()
            .MapPost("/{id:guid}/print", PrintSessionLabelsAsync)
            .WithName("CheckInPrintSessionLabels")
            .WithSummary("Compile and dispatch badge and receipt labels for a check-in session")
            .WithDescription(
                "Compiles participant badges and guardian receipts, dispatching server TCP raw-socket print jobs or returning ZPL for client WebUSB/local print."
            )
            .Produces<PrintCheckInSessionLabelsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CheckInLabelResponse>>, ProblemHttpResult>
    > ListLabelsAsync(
        CheckInLabelKind? labelKind,
        Guid? configurationId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCheckInLabelsQuery,
            IReadOnlyList<CheckInLabelResponse>
        >(
            new GetCheckInLabelsQuery(labelKind, configurationId, activeOnly ?? false),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInLabelResponse>, ProblemHttpResult>
    > GetLabelByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetCheckInLabelByIdQuery, CheckInLabelResponse>(
            new GetCheckInLabelByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CheckInLabelResponse>, ProblemHttpResult>
    > CreateLabelAsync(
        CreateCheckInLabelCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateCheckInLabelCommand, CheckInLabelResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/checkin/labels/{result.Value.Id:D}", result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInLabelResponse>, ProblemHttpResult>
    > UpdateLabelAsync(
        Guid id,
        UpdateCheckInLabelCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateCheckInLabelCommand, CheckInLabelResponse>(
            command with
            {
                LabelId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteLabelAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteCheckInLabelCommand, bool>(
            new DeleteCheckInLabelCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CheckInLabelPreviewResponse>, ProblemHttpResult>
    > PreviewLabelAsync(
        PreviewCheckInLabelCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PreviewCheckInLabelCommand,
            CheckInLabelPreviewResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PrintCheckInSessionLabelsResponse>, ProblemHttpResult>
    > PrintSessionLabelsAsync(
        Guid id,
        PrintCheckInSessionLabelsCommand? command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var normalized = (command ?? new PrintCheckInSessionLabelsCommand(id)) with
        {
            SessionId = id,
        };
        var result = await dispatcher.SendAsync<
            PrintCheckInSessionLabelsCommand,
            PrintCheckInSessionLabelsResponse
        >(normalized, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CheckInProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
