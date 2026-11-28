using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal static class FinancialTransactionEndpoints
{
    public static IEndpointRouteBuilder MapFinancialTransactionEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var txGroup = endpoints
            .MapGroup("/api/finance/transactions")
            .WithTags("Finance Transactions")
            .RequireAuthorization();

        MapTransactionCrudEndpoints(txGroup);
        MapRefundAndImageEndpoints(txGroup);
        MapScannerEndpoints(endpoints);

        return endpoints;
    }

    private static void MapTransactionCrudEndpoints(RouteGroupBuilder txGroup)
    {
        _ = txGroup
            .MapGet("/", ListAsync)
            .WithName("FinanceListTransactions")
            .WithSummary("List financial transactions and multi-account splits")
            .WithDescription(
                "Returns settled, pending, and refunded transactions across funds, dues, fees, and invoices."
            )
            .Produces<IReadOnlyList<FinancialTransactionResponse>>(StatusCodes.Status200OK);

        _ = txGroup
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("FinanceGetTransactionById")
            .WithSummary("Get financial transaction by ID")
            .WithDescription(
                "Returns transaction details including account splits, check image metadata, and refunds."
            )
            .Produces<FinancialTransactionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = txGroup
            .MapPost("/", RecordAsync)
            .WithName("FinanceRecordTransaction")
            .WithSummary("Record a financial transaction or gateway charge")
            .WithDescription(
                "Records multi-account splits, optionally charging IPaymentGateway and settling open invoices."
            )
            .Produces<FinancialTransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapRefundAndImageEndpoints(RouteGroupBuilder txGroup)
    {
        _ = txGroup
            .MapPost("/{id:guid}/refunds", RefundAsync)
            .WithName("FinanceRefundTransaction")
            .WithSummary("Process a full or partial refund")
            .WithDescription(
                "Processes a refund, optionally calling IPaymentGateway.RefundAsync and reversing settled invoice balances."
            )
            .Produces<FinancialTransactionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = txGroup
            .MapPost("/{id:guid}/images", UploadImageAsync)
            .WithName("FinanceUploadCheckImage")
            .WithSummary("Encrypt and attach a check or remittance image")
            .WithDescription(
                "Encrypts the image with AES-256-GCM before uploading to RustFS object storage."
            )
            .Produces<FinancialTransactionImageResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = txGroup
            .MapGet("/{id:guid}/images/{imageId:guid}", DownloadImageAsync)
            .WithName("FinanceDownloadCheckImage")
            .WithSummary("Download and decrypt a check or remittance image")
            .WithDescription(
                "Streams and verifies AES-256-GCM authenticated check image bytes from RustFS."
            )
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static void MapScannerEndpoints(IEndpointRouteBuilder endpoints)
    {
        var scannerGroup = endpoints
            .MapGroup("/api/finance/check-scanner")
            .WithTags("Finance Check Scanner")
            .RequireAuthorization();

        _ = scannerGroup
            .MapPost("/ingest", IngestScannedCheckAsync)
            .WithName("FinanceIngestScannedCheck")
            .WithSummary("Ingest a scanned paper check with MICR matching and encrypted images")
            .WithDescription(
                "Validates ABA routing checksum, matches hashed bank account identifiers to constituents, encrypts check images, and posts to an open scanner batch."
            )
            .Produces<CheckScannerIngestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialTransactionResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? batchId,
        Guid? personId,
        Guid? accountId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialTransactionsQuery,
            IReadOnlyList<FinancialTransactionResponse>
        >(
            new GetFinancialTransactionsQuery(
                batchId,
                personId,
                accountId,
                fromUtc,
                toUtc,
                maxResults ?? 100
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialTransactionResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialTransactionByIdQuery,
            FinancialTransactionResponse
        >(new GetFinancialTransactionByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialTransactionResponse>, ProblemHttpResult>
    > RecordAsync(
        RecordFinancialTransactionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/transactions/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialTransactionResponse>, ProblemHttpResult>
    > RefundAsync(
        Guid id,
        RefundFinancialTransactionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RefundFinancialTransactionCommand,
            FinancialTransactionResponse
        >(command with { TransactionId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialTransactionImageResponse>, ProblemHttpResult>
    > UploadImageAsync(
        Guid id,
        UploadCheckImageCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UploadCheckImageCommand,
            FinancialTransactionImageResponse
        >(command with { TransactionId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/transactions/{id:D}/images/{result.Value.Id:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> DownloadImageAsync(
        Guid id,
        Guid imageId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            DownloadDecryptedCheckImageQuery,
            DecryptedCheckImageResponse
        >(new DownloadDecryptedCheckImageQuery(id, imageId), cancellationToken);
        if (!result.IsSuccess)
        {
            return FinanceProblemResultMapper.ToProblemResult(result, httpContext);
        }

        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        httpContext.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        httpContext.Response.Headers.CacheControl = "private, no-store";
        var ext = result.Value.ContentType switch
        {
            "image/jpeg" => "jpg",
            "image/tiff" => "tif",
            "application/pdf" => "pdf",
            _ => "png",
        };
        return TypedResults.Bytes(
            result.Value.PlaintextBytes,
            result.Value.ContentType,
            fileDownloadName: $"check-{id:N}-{imageId:N}.{ext}"
        );
    }

    private static async Task<
        Results<Created<CheckScannerIngestResponse>, ProblemHttpResult>
    > IngestScannedCheckAsync(
        IngestScannedCheckCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            IngestScannedCheckCommand,
            CheckScannerIngestResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/transactions/{result.Value.Transaction.Id:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
