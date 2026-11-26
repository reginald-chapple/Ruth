using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Statements;

internal static class FinancialStatementEndpoints
{
    public static IEndpointRouteBuilder MapFinancialStatementEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/statements")
            .WithTags("Finance Statements & Tax Receipts")
            .RequireAuthorization();

        MapTemplateRoutes(group);
        MapGenerationRoutes(group);
        return endpoints;
    }

    private static void MapTemplateRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/templates", ListTemplatesAsync)
            .WithName("FinanceListStatementTemplates")
            .WithSummary("List financial statement and tax receipt templates")
            .WithDescription(
                "Returns configured Razor + merge-token templates separating tax-deductible receipts from non-deductible statements."
            )
            .Produces<IReadOnlyList<FinancialStatementTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/templates/{id:guid}", GetTemplateByIdAsync)
            .WithName("FinanceGetStatementTemplateById")
            .WithSummary("Get financial statement template by ID")
            .WithDescription(
                "Returns full configuration details for a statement or tax receipt template."
            )
            .Produces<FinancialStatementTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/templates", CreateTemplateAsync)
            .WithName("FinanceCreateStatementTemplate")
            .WithSummary("Create a financial statement or tax receipt template")
            .WithDescription(
                "Creates a statement template with validated print CSS and merge header/footer markup."
            )
            .Produces<FinancialStatementTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/templates/{id:guid}", UpdateTemplateAsync)
            .WithName("FinanceUpdateStatementTemplate")
            .WithSummary("Update a financial statement or tax receipt template")
            .WithDescription("Updates an existing statement or tax receipt template.")
            .Produces<FinancialStatementTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapGenerationRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/generate", GenerateStatementAsync)
            .WithName("FinanceGenerateStatement")
            .WithSummary("Generate a tax receipt or account statement")
            .WithDescription(
                "Renders a print-CSS HTML and PDF 1.4 statement or tax receipt for an individual, household, or organization sponsor."
            )
            .Produces<RenderedFinancialStatementDocumentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/batch-run", RunBatchStatementsAsync)
            .WithName("FinanceRunStatementBatch")
            .WithSummary("Execute a batch annual tax receipt or account statement run")
            .WithDescription(
                "Generates statements across qualifying constituents, consolidated households, and corporate sponsors."
            )
            .Produces<FinancialStatementBatchRunResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/", ListGeneratedStatementsAsync)
            .WithName("FinanceListGeneratedStatements")
            .WithSummary("List generated tax receipts and account statements")
            .WithDescription(
                "Returns previously generated financial statement and tax receipt metadata."
            )
            .Produces<IReadOnlyList<GeneratedFinancialStatementResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}/download", DownloadStatementAsync)
            .WithName("FinanceDownloadStatement")
            .WithSummary("Download a generated statement or tax receipt as PDF or HTML")
            .WithDescription(
                "Streams the archived or rendered PDF 1.4 or HTML5 print document for a generated statement."
            )
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialStatementTemplateResponse>>, ProblemHttpResult>
    > ListTemplatesAsync(
        FinancialStatementKind? statementKind,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetFinancialStatementTemplatesQuery,
            IReadOnlyList<FinancialStatementTemplateResponse>
        >(
            new GetFinancialStatementTemplatesQuery(statementKind, activeOnly ?? false),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialStatementTemplateResponse>, ProblemHttpResult>
    > GetTemplateByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetFinancialStatementTemplateByIdQuery,
            FinancialStatementTemplateResponse
        >(new GetFinancialStatementTemplateByIdQuery(id), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<FinancialStatementTemplateResponse>, ProblemHttpResult>
    > CreateTemplateAsync(
        FinancialStatementTemplateSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateFinancialStatementTemplateCommand,
            FinancialStatementTemplateResponse
        >(new CreateFinancialStatementTemplateCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/finance/statements/templates/{res.Value.Id:D}", res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialStatementTemplateResponse>, ProblemHttpResult>
    > UpdateTemplateAsync(
        Guid id,
        FinancialStatementTemplateSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateFinancialStatementTemplateCommand,
            FinancialStatementTemplateResponse
        >(new UpdateFinancialStatementTemplateCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<RenderedFinancialStatementDocumentResponse>, ProblemHttpResult>
    > GenerateStatementAsync(
        GenerateFinancialStatementCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            GenerateFinancialStatementCommand,
            RenderedFinancialStatementDocumentResponse
        >(command, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/statements/{res.Value.Statement.Id:D}/download",
                res.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialStatementBatchRunResponse>, ProblemHttpResult>
    > RunBatchStatementsAsync(
        RunFinancialStatementBatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            RunFinancialStatementBatchCommand,
            FinancialStatementBatchRunResponse
        >(command, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<GeneratedFinancialStatementResponse>>, ProblemHttpResult>
    > ListGeneratedStatementsAsync(
        Guid? targetPersonId,
        Guid? targetHouseholdId,
        Guid? batchRunId,
        FinancialStatementKind? statementKind,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetGeneratedFinancialStatementsQuery,
            IReadOnlyList<GeneratedFinancialStatementResponse>
        >(
            new GetGeneratedFinancialStatementsQuery(
                targetPersonId,
                targetHouseholdId,
                batchRunId,
                statementKind,
                limit ?? 100
            ),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<FileContentHttpResult, ProblemHttpResult>
    > DownloadStatementAsync(
        Guid id,
        StatementDownloadFormat? format,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            DownloadFinancialStatementQuery,
            FinancialStatementDownloadResponse
        >(
            new DownloadFinancialStatementQuery(id, format ?? StatementDownloadFormat.Pdf),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.File(res.Value.ContentBytes, res.Value.ContentType, res.Value.FileName)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
