using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal static class AssessmentAndCheckEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentAndCheckEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        MapAssessmentRoutes(endpoints);
        MapBackgroundCheckRoutes(endpoints);
        return endpoints;
    }

    private static void MapAssessmentRoutes(IEndpointRouteBuilder endpoints)
    {
        var assessments = endpoints
            .MapGroup("/api/crm/assessments")
            .WithTags("CRM Assessments")
            .RequireAuthorization();

        _ = assessments
            .MapGet("/types", GetAssessmentTypesAsync)
            .WithName("CrmGetAssessmentTypes")
            .WithSummary("List configured assessment types")
            .Produces<IReadOnlyList<AssessmentTypeResponse>>(StatusCodes.Status200OK);

        _ = assessments
            .MapPost("/types", CreateAssessmentTypeAsync)
            .WithName("CrmCreateAssessmentType")
            .WithSummary("Create a custom assessment type")
            .Produces<AssessmentTypeResponse>(StatusCodes.Status201Created);

        _ = assessments
            .MapGet("/person/{personId:guid}", GetPersonAssessmentsAsync)
            .WithName("CrmGetPersonAssessments")
            .WithSummary("List assessments for a person")
            .Produces<IReadOnlyList<AssessmentResponse>>(StatusCodes.Status200OK);

        _ = assessments
            .MapPost("/request", RequestAssessmentAsync)
            .WithName("CrmRequestAssessment")
            .WithSummary("Request or self-initiate an assessment")
            .Produces<AssessmentResponse>(StatusCodes.Status201Created);

        _ = assessments
            .MapPost("/{id:guid}/complete", CompleteAssessmentAsync)
            .WithName("CrmCompleteAssessment")
            .WithSummary("Record structured JSON assessment completion results")
            .Produces<AssessmentResponse>(StatusCodes.Status200OK);
    }

    private static void MapBackgroundCheckRoutes(IEndpointRouteBuilder endpoints)
    {
        var checks = endpoints
            .MapGroup("/api/crm/background-checks")
            .WithTags("CRM Background Checks")
            .RequireAuthorization();

        _ = checks
            .MapGet("/packages", GetPackagesAsync)
            .WithName("CrmGetBackgroundCheckPackages")
            .WithSummary("List screening packages across registered background check providers")
            .Produces<IReadOnlyList<BackgroundCheckPackageResponse>>(StatusCodes.Status200OK);

        _ = checks
            .MapGet("/person/{personId:guid}", GetPersonChecksAsync)
            .WithName("CrmGetPersonBackgroundChecks")
            .WithSummary("List background check history for a person")
            .Produces<IReadOnlyList<BackgroundCheckResponse>>(StatusCodes.Status200OK);

        _ = checks
            .MapPost("/request", RequestCheckAsync)
            .WithName("CrmRequestBackgroundCheck")
            .WithSummary("Initiate a background check through a registered modular provider")
            .Produces<BackgroundCheckResponse>(StatusCodes.Status201Created);

        _ = endpoints
            .MapPost("/api/crm/webhooks/background-checks/{providerKey}", ReceiveWebhookAsync)
            .WithTags("CRM Background Checks")
            .AllowAnonymous()
            .WithName("CrmReceiveBackgroundCheckWebhook")
            .WithSummary("Receive and HMAC-verify a background check provider webhook callback")
            .Produces<BackgroundCheckResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AssessmentTypeResponse>>, ProblemHttpResult>
    > GetAssessmentTypesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAssessmentTypesQuery,
            IReadOnlyList<AssessmentTypeResponse>
        >(new GetAssessmentTypesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AssessmentTypeResponse>, ProblemHttpResult>
    > CreateAssessmentTypeAsync(
        CreateAssessmentTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateAssessmentTypeCommand,
            AssessmentTypeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/assessments/types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AssessmentResponse>>, ProblemHttpResult>
    > GetPersonAssessmentsAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonAssessmentsQuery,
            IReadOnlyList<AssessmentResponse>
        >(new GetPersonAssessmentsQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AssessmentResponse>, ProblemHttpResult>
    > RequestAssessmentAsync(
        RequestAssessmentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RequestAssessmentCommand, AssessmentResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/assessments/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AssessmentResponse>, ProblemHttpResult>
    > CompleteAssessmentAsync(
        Guid id,
        CompleteAssessmentCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CompleteAssessmentCommand, AssessmentResponse>(
            command with
            {
                AssessmentId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BackgroundCheckPackageResponse>>, ProblemHttpResult>
    > GetPackagesAsync(
        string? providerKey,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetBackgroundCheckPackagesQuery,
            IReadOnlyList<BackgroundCheckPackageResponse>
        >(new GetBackgroundCheckPackagesQuery(providerKey), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BackgroundCheckResponse>>, ProblemHttpResult>
    > GetPersonChecksAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonBackgroundChecksQuery,
            IReadOnlyList<BackgroundCheckResponse>
        >(new GetPersonBackgroundChecksQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<BackgroundCheckResponse>, ProblemHttpResult>
    > RequestCheckAsync(
        RequestBackgroundCheckCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RequestBackgroundCheckCommand,
            BackgroundCheckResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/background-checks/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<BackgroundCheckResponse>, ProblemHttpResult>
    > ReceiveWebhookAsync(
        string providerKey,
        IBackgroundCheckProviderResolver providerResolver,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        var provider = providerResolver.Resolve(providerKey);
        var signatureHeaderName = provider?.WebhookSignatureHeaderName ?? "X-Checkr-Signature";
        var signature = httpContext.Request.Headers[signatureHeaderName].FirstOrDefault();

        var result = await dispatcher.SendAsync<
            ProcessBackgroundCheckWebhookCommand,
            BackgroundCheckResponse
        >(
            new ProcessBackgroundCheckWebhookCommand(providerKey, rawPayload, signature),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
