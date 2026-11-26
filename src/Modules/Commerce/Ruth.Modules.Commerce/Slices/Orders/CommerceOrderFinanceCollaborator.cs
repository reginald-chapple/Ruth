using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class CommerceOrderFinanceCollaborator(ICommandDispatcher commandDispatcher)
{
    public async Task<Result<Guid>> EnsureConstituentPersonAsync(
        Guid? existingPersonId,
        string fullName,
        string email,
        Guid? branchId,
        CancellationToken cancellationToken
    )
    {
        if (existingPersonId is { } id && id != Guid.Empty)
        {
            return id;
        }

        var (firstName, lastName) = SplitName(fullName);
        var created = await commandDispatcher.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                firstName,
                lastName,
                Email: email.Trim(),
                PrimaryBranchId: branchId
            ),
            cancellationToken
        );
        return created.IsFailure ? Result.Failure<Guid>(created.Errors) : created.Value.Id;
    }

    public async Task<Result> SettleOrderAsync(
        CommerceOrder order,
        EvaluatedCommerceOrderPlan plan,
        SubmitCommerceOrderCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        return plan.GrandTotalAmount <= 0m ? Result.Success()
            : command.RequestInvoiceBilling
                ? await IssueOrderInvoiceAsync(order, plan, nowUtc, cancellationToken)
            : await ChargeOrderSplitsAsync(order, plan, command, nowUtc, cancellationToken);
    }

    public async Task<Result> RefundOrderTransactionAsync(
        CommerceOrder order,
        decimal amount,
        string reason,
        Guid? processedByPersonId,
        CancellationToken cancellationToken
    )
    {
        if (order.LinkedFinancialTransactionId is not { } txId)
        {
            return CommerceErrors.OrderStateTransitionInvalid;
        }

        var res = await commandDispatcher.SendAsync<
            RefundFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RefundFinancialTransactionCommand(txId, amount, reason, processedByPersonId),
            cancellationToken
        );
        return res.IsFailure ? Result.Failure(res.Errors) : Result.Success();
    }

    private async Task<Result> IssueOrderInvoiceAsync(
        CommerceOrder order,
        EvaluatedCommerceOrderPlan plan,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var personRes = await EnsureConstituentPersonAsync(
            order.BuyerPersonId,
            order.BuyerName,
            order.BuyerEmail,
            order.BranchId,
            cancellationToken
        );
        if (personRes.IsFailure)
        {
            return Result.Failure(personRes.Errors);
        }

        var issueDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        InvoiceLineItemSpec[] lineItems =
        [
            .. plan.AccountSplits.Select(s => new InvoiceLineItemSpec(
                s.AccountId,
                $"{s.Summary} ({order.OrderNumber})",
                1m,
                s.Amount
            )),
        ];

        var invRes = await commandDispatcher.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            new CreateInvoiceCommand(
                personRes.Value,
                issueDate,
                issueDate.AddDays(30),
                order.CurrencyCode,
                lineItems,
                IssueImmediately: true,
                BranchId: order.BranchId,
                Memo: $"Commerce order {order.OrderNumber}"
            ),
            cancellationToken
        );
        if (invRes.IsFailure)
        {
            return Result.Failure(invRes.Errors);
        }

        order.AttachInvoice(invRes.Value.Id);
        return Result.Success();
    }

    private async Task<Result> ChargeOrderSplitsAsync(
        CommerceOrder order,
        EvaluatedCommerceOrderPlan plan,
        SubmitCommerceOrderCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var hasElectronic =
            !string.IsNullOrWhiteSpace(command.OneTimeHostedPaymentToken)
            || command.SavedPaymentMethodId.HasValue;
        var hasOfflineCode = !string.IsNullOrWhiteSpace(command.ExternalTransactionCode);
        if (!hasElectronic && !hasOfflineCode)
        {
            return CommerceErrors.OrderCartInvalid;
        }

        var gatewayId = hasElectronic
            ? command.FinancialGatewayId
                ?? plan.Lines.Select(static l => l.Product.DefaultFinancialGatewayId)
                    .FirstOrDefault(static g => g.HasValue)
            : null;

        FinancialTransactionSplitSpec[] splits =
        [
            .. plan.AccountSplits.Select(s => new FinancialTransactionSplitSpec(
                s.AccountId,
                s.Amount,
                InvoiceId: null,
                Summary: $"{s.Summary} ({order.OrderNumber})"
            )),
        ];

        var txRes = await commandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                order.BuyerPersonId,
                order.CurrencyCode,
                hasElectronic
                    ? FinancialTransactionSourceType.Website
                    : FinancialTransactionSourceType.StaffEntry,
                hasElectronic ? FinancialTenderType.CreditCard : FinancialTenderType.Check,
                splits,
                FinancialGatewayId: gatewayId,
                SavedPaymentMethodId: command.SavedPaymentMethodId,
                OneTimeHostedToken: command.OneTimeHostedPaymentToken,
                TransactionCode: command.ExternalTransactionCode,
                TransactionDateTimeUtc: nowUtc,
                Summary: $"Commerce order {order.OrderNumber}"
            ),
            cancellationToken
        );
        if (txRes.IsFailure)
        {
            return Result.Failure(txRes.Errors);
        }

        order.MarkPaid(txRes.Value.Id, txRes.Value.TotalAmount);
        return Result.Success();
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        var spaceIdx = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return spaceIdx <= 0
            ? (trimmed, "Constituent")
            : (trimmed[..spaceIdx].Trim(), trimmed[(spaceIdx + 1)..].Trim());
    }
}
