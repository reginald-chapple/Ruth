using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.Modules.Finance.Slices.Batches;
using Ruth.Modules.Finance.Slices.Invoices;
using Ruth.Modules.Finance.Slices.Pledges;
using Ruth.Modules.Finance.Slices.Transactions;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S3SliceRegistration
{
    public static void RegisterW4S3Services(IServiceCollection services)
    {
        _ = services.AddScoped<IFinanceLedgerSummaryReader, DbFinanceLedgerSummaryReader>();
        services.TryAddScoped<BatchReconciliationReader>();
        services.TryAddScoped<TransactionPostingCollaborator>();
        services.TryAddScoped<ScannedCheckImageAttacher>();
        services.TryAddScoped<ICheckImageVault, AesGcmCheckImageVault>();

        RegisterInvoiceHandlers(services);
        RegisterBatchHandlers(services);
        RegisterTransactionHandlers(services);
    }

    private static void RegisterInvoiceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateInvoiceCommand,
            InvoiceResponse,
            CreateInvoiceCommandHandler
        >();
        _ = services.AddCommandHandler<
            IssueInvoiceCommand,
            InvoiceResponse,
            IssueInvoiceCommandHandler
        >();
        _ = services.AddCommandHandler<
            VoidInvoiceCommand,
            InvoiceResponse,
            VoidInvoiceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetInvoicesQuery,
            IReadOnlyList<InvoiceResponse>,
            GetInvoicesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetInvoiceByIdQuery,
            InvoiceResponse,
            GetInvoiceByIdQueryHandler
        >();
    }

    private static void RegisterBatchHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFinancialBatchCommand,
            FinancialBatchResponse,
            CreateFinancialBatchCommandHandler
        >();
        _ = services.AddCommandHandler<
            OpenFinancialBatchCommand,
            FinancialBatchResponse,
            OpenFinancialBatchCommandHandler
        >();
        _ = services.AddCommandHandler<
            CloseFinancialBatchCommand,
            FinancialBatchResponse,
            CloseFinancialBatchCommandHandler
        >();
        _ = services.AddCommandHandler<
            ReopenFinancialBatchCommand,
            FinancialBatchResponse,
            ReopenFinancialBatchCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialBatchesQuery,
            IReadOnlyList<FinancialBatchResponse>,
            GetFinancialBatchesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialBatchByIdQuery,
            FinancialBatchResponse,
            GetFinancialBatchByIdQueryHandler
        >();
    }

    private static void RegisterTransactionHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse,
            RecordFinancialTransactionCommandHandler
        >();
        _ = services.AddCommandHandler<
            RefundFinancialTransactionCommand,
            FinancialTransactionResponse,
            RefundFinancialTransactionCommandHandler
        >();
        _ = services.AddCommandHandler<
            UploadCheckImageCommand,
            FinancialTransactionImageResponse,
            UploadCheckImageCommandHandler
        >();
        _ = services.AddQueryHandler<
            DownloadDecryptedCheckImageQuery,
            DecryptedCheckImageResponse,
            DownloadDecryptedCheckImageQueryHandler
        >();
        _ = services.AddCommandHandler<
            IngestScannedCheckCommand,
            CheckScannerIngestResponse,
            IngestScannedCheckCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialTransactionsQuery,
            IReadOnlyList<FinancialTransactionResponse>,
            GetFinancialTransactionsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialTransactionByIdQuery,
            FinancialTransactionResponse,
            GetFinancialTransactionByIdQueryHandler
        >();
    }
}
