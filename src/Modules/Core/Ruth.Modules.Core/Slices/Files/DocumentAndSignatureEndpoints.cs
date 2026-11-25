using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Files;

internal static class DocumentAndSignatureEndpoints
{
    public static IEndpointRouteBuilder MapDocumentAndSignatureEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var docs = endpoints
            .MapGroup("/api/core/documents")
            .WithTags("Documents")
            .RequireAuthorization();
        MapDocumentRoutes(docs);

        var sigs = endpoints
            .MapGroup("/api/core/signatures")
            .WithTags("Electronic Signatures")
            .RequireAuthorization();
        MapSignatureRoutes(sigs);

        return endpoints;
    }

    private static void MapDocumentRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/types", GetDocumentTypesAsync)
            .WithName("CoreGetDocumentTypes")
            .WithSummary("List entity-scoped document types")
            .Produces<IReadOnlyList<DocumentTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/types", CreateDocumentTypeAsync)
            .WithName("CoreCreateDocumentType")
            .WithSummary("Create a document type")
            .Produces<DocumentTypeResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/", GetDocumentsAsync)
            .WithName("CoreGetDocuments")
            .WithSummary("List documents attached to an entity")
            .Produces<IReadOnlyList<DocumentResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateDocumentAsync)
            .WithName("CoreCreateDocument")
            .WithSummary("Attach a binary file as a permanent document")
            .Produces<DocumentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapDelete("/{id:guid}", DeleteDocumentAsync)
            .WithName("CoreDeleteDocument")
            .WithSummary("Delete a document and mark its binary file temporary")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapSignatureRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/templates", GetTemplatesAsync)
            .WithName("CoreGetSignatureDocumentTemplates")
            .WithSummary("List electronic signature document templates")
            .Produces<IReadOnlyList<SignatureDocumentTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/templates", CreateTemplateAsync)
            .WithName("CoreCreateSignatureDocumentTemplate")
            .WithSummary("Create an electronic signature document template")
            .Produces<SignatureDocumentTemplateResponse>(StatusCodes.Status201Created);

        _ = group
            .MapGet("/documents", GetSignatureDocumentsAsync)
            .WithName("CoreGetSignatureDocuments")
            .WithSummary("List electronic signature documents")
            .Produces<IReadOnlyList<SignatureDocumentResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/documents", CreateSignatureDocumentAsync)
            .WithName("CoreCreateSignatureDocument")
            .WithSummary("Create a pending electronic signature document")
            .Produces<SignatureDocumentResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPost("/documents/{id:guid}/sign", SignDocumentAsync)
            .WithName("CoreSignSignatureDocument")
            .WithSummary("Capture electronic signature and seal document with verification hash")
            .Produces<SignatureDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DocumentTypeResponse>>, ProblemHttpResult>
    > GetDocumentTypesAsync(
        Guid? entityTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetDocumentTypesQuery,
            IReadOnlyList<DocumentTypeResponse>
        >(new GetDocumentTypesQuery(entityTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<DocumentTypeResponse>, ProblemHttpResult>
    > CreateDocumentTypeAsync(
        CreateDocumentTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateDocumentTypeCommand, DocumentTypeResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/documents/types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DocumentResponse>>, ProblemHttpResult>
    > GetDocumentsAsync(
        Guid? entityId,
        Guid? documentTypeId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetDocumentsQuery,
            IReadOnlyList<DocumentResponse>
        >(new GetDocumentsQuery(entityId, documentTypeId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<DocumentResponse>, ProblemHttpResult>
    > CreateDocumentAsync(
        CreateDocumentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<CreateDocumentCommand, DocumentResponse>(
            command with
            {
                RequestingUserId = command.RequestingUserId ?? userId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/documents/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteDocumentAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteDocumentCommand, bool>(
            new DeleteDocumentCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SignatureDocumentTemplateResponse>>, ProblemHttpResult>
    > GetTemplatesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSignatureDocumentTemplatesQuery,
            IReadOnlyList<SignatureDocumentTemplateResponse>
        >(new GetSignatureDocumentTemplatesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SignatureDocumentTemplateResponse>, ProblemHttpResult>
    > CreateTemplateAsync(
        CreateSignatureDocumentTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateSignatureDocumentTemplateCommand,
            SignatureDocumentTemplateResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/signatures/templates/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SignatureDocumentResponse>>, ProblemHttpResult>
    > GetSignatureDocumentsAsync(
        Guid? templateId,
        Guid? appliesToEntityId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSignatureDocumentsQuery,
            IReadOnlyList<SignatureDocumentResponse>
        >(new GetSignatureDocumentsQuery(templateId, appliesToEntityId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SignatureDocumentResponse>, ProblemHttpResult>
    > CreateSignatureDocumentAsync(
        CreateSignatureDocumentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateSignatureDocumentCommand,
            SignatureDocumentResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/signatures/documents/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SignatureDocumentResponse>, ProblemHttpResult>
    > SignDocumentAsync(
        Guid id,
        SignSignatureDocumentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var result = await dispatcher.SendAsync<
            SignSignatureDocumentCommand,
            SignatureDocumentResponse
        >(
            command with
            {
                SignatureDocumentId = id,
                SignedByUserId = command.SignedByUserId ?? userId,
                ClientIp = command.ClientIp ?? clientIp,
                ClientUserAgent = command.ClientUserAgent ?? userAgent,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static Guid? TryResolveUserId(HttpContext context)
    {
        return Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? parsed
            : null;
    }
}
