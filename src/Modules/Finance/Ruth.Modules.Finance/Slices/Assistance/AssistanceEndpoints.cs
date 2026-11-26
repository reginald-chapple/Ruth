using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal static class AssistanceEndpoints
{
    public static IEndpointRouteBuilder MapAssistanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/assistance")
            .WithTags("Finance Assistance & Grants")
            .RequireAuthorization();

        MapProgramRoutes(group);
        MapCaseQueryAndIntakeRoutes(group);
        MapCaseLifecycleRoutes(group);
        return endpoints;
    }

    private static void MapProgramRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/programs", ListProgramsAsync)
            .WithName("FinanceListAssistancePrograms")
            .WithSummary("List assistance, scholarship, fee-waiver, and micro-grant programs")
            .WithDescription("Returns configured assistance and grant programs with budget totals.")
            .Produces<IReadOnlyList<AssistanceProgramResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/programs", CreateProgramAsync)
            .WithName("FinanceCreateAssistanceProgram")
            .WithSummary("Create an assistance or grant program")
            .WithDescription(
                "Creates an assistance, scholarship, fee-waiver, or micro-grant program backed by a funding account."
            )
            .Produces<AssistanceProgramResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/programs/{id:guid}", UpdateProgramAsync)
            .WithName("FinanceUpdateAssistanceProgram")
            .WithSummary("Update an assistance or grant program")
            .WithDescription("Updates budget, award ceiling, or metadata on an assistance program.")
            .Produces<AssistanceProgramResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapCaseQueryAndIntakeRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/cases", ListCasesAsync)
            .WithName("FinanceListAssistanceCases")
            .WithSummary("List assistance and grant cases")
            .WithDescription(
                "Returns assistance cases filtered optionally by program, applicant, status, or branch."
            )
            .Produces<IReadOnlyList<AssistanceCaseResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/cases/{id:guid}", GetCaseByIdAsync)
            .WithName("FinanceGetAssistanceCaseById")
            .WithSummary("Get assistance case by ID")
            .WithDescription(
                "Returns complete case intake, review, document, and disbursement details."
            )
            .Produces<AssistanceCaseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/cases", SubmitCaseAsync)
            .WithName("FinanceSubmitAssistanceCase")
            .WithSummary("Submit an assistance or grant case")
            .WithDescription("Creates an intake case under an open assistance or grant program.")
            .Produces<AssistanceCaseResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCaseLifecycleRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/cases/{id:guid}/documents", AttachDocumentAsync)
            .WithName("FinanceAttachAssistanceDocument")
            .WithSummary("Attach a verification document to an assistance case")
            .WithDescription("Attaches intake or eligibility verification documentation to a case.")
            .Produces<AssistanceCaseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/cases/{id:guid}/review", ReviewCaseAsync)
            .WithName("FinanceReviewAssistanceCase")
            .WithSummary("Review, approve, or deny an assistance case")
            .WithDescription(
                "Transitions a case to UnderReview, Approved (enforcing verified documents and award ceilings), or Denied."
            )
            .Produces<AssistanceCaseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/cases/{id:guid}/disbursements", RecordDisbursementAsync)
            .WithName("FinanceRecordAssistanceDisbursement")
            .WithSummary("Record a disbursement or invoice fee-waiver credit")
            .WithDescription(
                "Disburses approved funds via check, ACH, vendor payment, or internal invoice credit."
            )
            .Produces<AssistanceCaseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/cases/{id:guid}/close", CloseCaseAsync)
            .WithName("FinanceCloseAssistanceCase")
            .WithSummary("Close an assistance case")
            .WithDescription("Administratively closes an assistance or grant case.")
            .Produces<AssistanceCaseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AssistanceProgramResponse>>, ProblemHttpResult>
    > ListProgramsAsync(
        AssistanceProgramCategory? category,
        Guid? branchId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetAssistanceProgramsQuery,
            IReadOnlyList<AssistanceProgramResponse>
        >(
            new GetAssistanceProgramsQuery(category, branchId, activeOnly ?? false),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<AssistanceProgramResponse>, ProblemHttpResult>
    > CreateProgramAsync(
        AssistanceProgramSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateAssistanceProgramCommand,
            AssistanceProgramResponse
        >(new CreateAssistanceProgramCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/finance/assistance/programs/{res.Value.Id:D}", res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceProgramResponse>, ProblemHttpResult>
    > UpdateProgramAsync(
        Guid id,
        AssistanceProgramSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateAssistanceProgramCommand,
            AssistanceProgramResponse
        >(new UpdateAssistanceProgramCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AssistanceCaseResponse>>, ProblemHttpResult>
    > ListCasesAsync(
        Guid? programId,
        Guid? applicantPersonId,
        AssistanceCaseStatus? status,
        Guid? branchId,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetAssistanceCasesQuery,
            IReadOnlyList<AssistanceCaseResponse>
        >(
            new GetAssistanceCasesQuery(
                programId,
                applicantPersonId,
                status,
                branchId,
                limit ?? 100
            ),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceCaseResponse>, ProblemHttpResult>
    > GetCaseByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetAssistanceCaseByIdQuery, AssistanceCaseResponse>(
            new GetAssistanceCaseByIdQuery(id),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<AssistanceCaseResponse>, ProblemHttpResult>
    > SubmitCaseAsync(
        SubmitAssistanceCaseCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<SubmitAssistanceCaseCommand, AssistanceCaseResponse>(
            command,
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created($"/api/finance/assistance/cases/{res.Value.Id:D}", res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceCaseResponse>, ProblemHttpResult>
    > AttachDocumentAsync(
        Guid id,
        AttachAssistanceDocumentCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            AttachAssistanceDocumentCommand,
            AssistanceCaseResponse
        >(body with { CaseId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceCaseResponse>, ProblemHttpResult>
    > ReviewCaseAsync(
        Guid id,
        ReviewAssistanceCaseCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<ReviewAssistanceCaseCommand, AssistanceCaseResponse>(
            body with
            {
                CaseId = id,
            },
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceCaseResponse>, ProblemHttpResult>
    > RecordDisbursementAsync(
        Guid id,
        RecordAssistanceDisbursementCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            RecordAssistanceDisbursementCommand,
            AssistanceCaseResponse
        >(body with { CaseId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<AssistanceCaseResponse>, ProblemHttpResult>
    > CloseCaseAsync(
        Guid id,
        CloseAssistanceCaseCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<CloseAssistanceCaseCommand, AssistanceCaseResponse>(
            body with
            {
                CaseId = id,
            },
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
