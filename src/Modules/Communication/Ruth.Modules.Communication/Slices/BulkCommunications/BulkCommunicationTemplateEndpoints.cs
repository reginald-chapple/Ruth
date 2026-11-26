using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal static class BulkCommunicationTemplateEndpoints
{
    public static IEndpointRouteBuilder MapBulkCommunicationTemplateEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/bulk-templates")
            .WithTags("Communication Bulk Templates & Email Builder")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListBulkTemplates")
            .WithSummary("List reusable bulk communication templates")
            .Produces<IReadOnlyList<CommunicationTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CommunicationGetBulkTemplateById")
            .WithSummary("Get a bulk communication template by identifier")
            .Produces<CommunicationTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CommunicationCreateBulkTemplate")
            .WithSummary(
                "Create a reusable bulk communication template with structured email sections"
            )
            .Produces<CommunicationTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CommunicationUpdateBulkTemplate")
            .WithSummary("Update an existing bulk communication template")
            .Produces<CommunicationTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CommunicationDeleteBulkTemplate")
            .WithSummary("Delete a non-system bulk communication template")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/preview-email", PreviewEmailAsync)
            .WithName("CommunicationPreviewEmailComposer")
            .WithSummary(
                "Preview compiled HTML and plain-text output from structured email sections"
            )
            .Produces<EmailComposerPreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommunicationTemplateResponse>>, ProblemHttpResult>
    > ListAsync(
        CommunicationChannel? channel,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationTemplatesQuery,
            IReadOnlyList<CommunicationTemplateResponse>
        >(new GetCommunicationTemplatesQuery(channel, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationTemplateResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationTemplateByIdQuery,
            CommunicationTemplateResponse
        >(new GetCommunicationTemplateByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CommunicationTemplateResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateCommunicationTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateCommunicationTemplateCommand,
            CommunicationTemplateResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/bulk-templates/{result.Value.Id:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationTemplateResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateCommunicationTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCommunicationTemplateCommand,
            CommunicationTemplateResponse
        >(command with { TemplateId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteCommunicationTemplateCommand, bool>(
            new DeleteCommunicationTemplateCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<EmailComposerPreviewResponse>, ProblemHttpResult>
    > PreviewEmailAsync(
        PreviewEmailComposerCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PreviewEmailComposerCommand,
            EmailComposerPreviewResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
