using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class RefundFinancialTransactionCommandHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry gatewayRegistry,
    ITenantContext tenantContext,
    IClock clock
) : ICommandHandler<RefundFinancialTransactionCommand, FinancialTransactionResponse>
{
    public async Task<Result<FinancialTransactionResponse>> HandleAsync(
        RefundFinancialTransactionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var txId = FinancialTransactionId.From(command.TransactionId);
        var tx = await dbContext
            .FinancialTransactions.Include(static t => t.Details)
            .Include(static t => t.Images)
            .Include(static t => t.Refunds)
            .FirstOrDefaultAsync(t => t.Id == txId, cancellationToken);
        if (tx is null)
        {
            return FinanceErrors.TransactionNotFound;
        }

        var gatewayRef = await ExecuteGatewayRefundIfNeededAsync(tx, command, cancellationToken);
        if (gatewayRef.IsFailure)
        {
            return Result.Failure<FinancialTransactionResponse>(gatewayRef.Errors);
        }

        var refunded = tx.RecordRefund(
            command.Amount,
            command.Reason,
            gatewayRef.Value,
            command.ProcessedByPersonId,
            clock.UtcNow
        );
        return refunded.IsFailure
            ? Result.Failure<FinancialTransactionResponse>(refunded.Errors)
            : await FinalizeRefundAsync(tx, refunded.Value, command.Amount, cancellationToken);
    }

    private async Task<FinancialTransactionResponse> FinalizeRefundAsync(
        FinancialTransaction tx,
        FinancialTransactionRefund refund,
        decimal requestedAmount,
        CancellationToken cancellationToken
    )
    {
        await ReverseLinkedInvoicesAsync(tx, requestedAmount, cancellationToken);

        dbContext.EnqueueIntegrationEvent(
            new FinancialTransactionRefundedIntegrationEvent(
                Guid.CreateVersion7(),
                clock.UtcNow,
                tx.Id.Value,
                refund.Id.Value,
                tx.AuthorizedPersonId,
                refund.Amount,
                tx.CurrencyCode,
                refund.Reason
            )
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var accountIds = tx.Details.Select(static d => d.AccountId).Distinct().ToArray();
        var names = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.Name, cancellationToken);

        return FinancialTransactionMapper.ToResponse(tx, names);
    }

    private async Task<Result<string?>> ExecuteGatewayRefundIfNeededAsync(
        FinancialTransaction tx,
        RefundFinancialTransactionCommand command,
        CancellationToken cancellationToken
    )
    {
        if (
            !command.ExecuteGatewayRefund
            || tx.FinancialGatewayId is null
            || string.IsNullOrWhiteSpace(tx.TransactionCode)
        )
        {
            return Result.Success<string?>(value: null);
        }

        var gateway = await dbContext
            .FinancialGateways.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == tx.FinancialGatewayId.Value, cancellationToken);
        if (gateway is null || !gatewayRegistry.TryGet(gateway.ProviderKey, out var provider))
        {
            return FinanceErrors.GatewayProviderUnavailable;
        }

        if (!gateway.IsTestMode && !tenantContext.IsLive)
        {
            return FinanceErrors.LiveChargeRequiresApprovedTenant;
        }

        var gatewayRefund = await provider.RefundAsync(
            new GatewayRefundRequest(
                tx.TransactionCode,
                command.Amount,
                tx.CurrencyCode,
                command.Reason,
                gateway.IsTestMode
            ),
            cancellationToken
        );
        return gatewayRefund.IsFailure
            ? Result.Failure<string?>(gatewayRefund.Errors)
            : gatewayRefund.Value.GatewayRefundId;
    }

    private async Task ReverseLinkedInvoicesAsync(
        FinancialTransaction tx,
        decimal refundAmount,
        CancellationToken cancellationToken
    )
    {
        var invoiceDetails = tx.Details.Where(static d => d.InvoiceId.HasValue).ToArray();
        if (invoiceDetails.Length == 0 || tx.TotalAmount <= 0m)
        {
            return;
        }

        var invoiceIds = invoiceDetails.Select(static d => d.InvoiceId!.Value).Distinct().ToArray();
        var invoices = await dbContext
            .Invoices.Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(static i => i.Id, cancellationToken);

        foreach (var detail in invoiceDetails)
        {
            if (invoices.TryGetValue(detail.InvoiceId!.Value, out var invoice))
            {
                var share = decimal.Round(
                    refundAmount * (detail.Amount / tx.TotalAmount),
                    2,
                    MidpointRounding.AwayFromZero
                );
                var reversal = Math.Min(share, invoice.PaidAmount);
                if (reversal > 0m)
                {
                    _ = invoice.ReversePayment(reversal);
                }
            }
        }
    }
}
