using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Templates;

internal static class TemplateEndpoints
{
    public static IEndpointRouteBuilder MapTemplateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        MapSnippetRoutes(endpoints);
        MapPreviewRoutes(endpoints);
        return endpoints;
    }

    private static void MapSnippetRoutes(IEndpointRouteBuilder endpoints)
    {
        var snippets = endpoints
            .MapGroup("/api/communication/snippets")
            .WithTags("Communication Templates")
            .RequireAuthorization();

        _ = snippets
            .MapGet("/", ListSnippetsAsync)
            .WithName("CommunicationListTemplateSnippets")
            .WithSummary("List reusable merge template snippets")
            .Produces<IReadOnlyList<TemplateSnippetResponse>>(StatusCodes.Status200OK);

        _ = snippets
            .MapGet("/{id:guid}", GetSnippetByIdAsync)
            .WithName("CommunicationGetTemplateSnippetById")
            .WithSummary("Get a template snippet by identifier")
            .Produces<TemplateSnippetResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = snippets
            .MapPost("/", CreateSnippetAsync)
            .WithName("CommunicationCreateTemplateSnippet")
            .WithSummary("Create a reusable merge template snippet")
            .Produces<TemplateSnippetResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = snippets
            .MapPut("/{id:guid}", UpdateSnippetAsync)
            .WithName("CommunicationUpdateTemplateSnippet")
            .WithSummary("Update an existing template snippet")
            .Produces<TemplateSnippetResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = snippets
            .MapDelete("/{id:guid}", DeleteSnippetAsync)
            .WithName("CommunicationDeleteTemplateSnippet")
            .WithSummary("Delete a non-system template snippet")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPreviewRoutes(IEndpointRouteBuilder endpoints)
    {
        var templates = endpoints
            .MapGroup("/api/communication/templates")
            .WithTags("Communication Templates")
            .RequireAuthorization();

        _ = templates
            .MapGet("/razor", ListRazorTemplatesAsync)
            .WithName("CommunicationListRazorTemplates")
            .WithSummary("List compile-time registered Razor component templates")
            .Produces<IReadOnlyList<RazorTemplateSummaryResponse>>(StatusCodes.Status200OK);

        _ = templates
            .MapPost("/preview-merge", PreviewMergeAsync)
            .WithName("CommunicationPreviewMergeTemplate")
            .WithSummary("Preview a safe declarative merge template against sample JSON context")
            .Produces<TemplatePreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = templates
            .MapPost("/preview-razor", PreviewRazorAsync)
            .WithName("CommunicationPreviewRazorTemplate")
            .WithSummary("Preview a registered compiled Razor template via Blazor HtmlRenderer")
            .Produces<TemplatePreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TemplateSnippetResponse>>, ProblemHttpResult>
    > ListSnippetsAsync(
        bool? activeOnly,
        string? search,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetTemplateSnippetsQuery,
            IReadOnlyList<TemplateSnippetResponse>
        >(new GetTemplateSnippetsQuery(activeOnly ?? false, search), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TemplateSnippetResponse>, ProblemHttpResult>
    > GetSnippetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetTemplateSnippetByIdQuery,
            TemplateSnippetResponse
        >(new GetTemplateSnippetByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<TemplateSnippetResponse>, ProblemHttpResult>
    > CreateSnippetAsync(
        CreateTemplateSnippetCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateTemplateSnippetCommand,
            TemplateSnippetResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/communication/snippets/{result.Value.Id:D}", result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TemplateSnippetResponse>, ProblemHttpResult>
    > UpdateSnippetAsync(
        Guid id,
        UpdateTemplateSnippetCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateTemplateSnippetCommand,
            TemplateSnippetResponse
        >(command with { SnippetId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteSnippetAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteTemplateSnippetCommand, bool>(
            new DeleteTemplateSnippetCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RazorTemplateSummaryResponse>>, ProblemHttpResult>
    > ListRazorTemplatesAsync(
        string? category,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetRegisteredRazorTemplatesQuery,
            IReadOnlyList<RazorTemplateSummaryResponse>
        >(new GetRegisteredRazorTemplatesQuery(category), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TemplatePreviewResponse>, ProblemHttpResult>
    > PreviewMergeAsync(
        PreviewMergeTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PreviewMergeTemplateCommand,
            TemplatePreviewResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TemplatePreviewResponse>, ProblemHttpResult>
    > PreviewRazorAsync(
        PreviewRazorTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PreviewRazorTemplateCommand,
            TemplatePreviewResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
