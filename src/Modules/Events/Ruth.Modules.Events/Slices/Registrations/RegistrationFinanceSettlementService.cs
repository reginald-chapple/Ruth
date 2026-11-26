using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class RegistrationFinanceSettlementService(ICommandDispatcher commandDispatcher)
{
    public async Task<Result> SettleInitialSubmissionAsync(
        Registration registration,
        RegistrationInstance instance,
        RegistrationTemplate template,
        SubmitRegistrationCommand command,
        decimal minimumInitialAmountDue,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (registration.TotalAmount <= 0m)
        {
            return Result.Success();
        }

        var useInvoice =
            template.PaymentModel == RegistrationPaymentModel.InvoiceBilling
            || (
                template.PaymentModel == RegistrationPaymentModel.DepositAndInstallments
                && command.RequestInvoiceBilling
            );

        if (useInvoice)
        {
            var invRes = await IssueRegistrationInvoiceAsync(
                registration,
                instance,
                template,
                nowUtc,
                cancellationToken
            );
            if (invRes.IsFailure)
            {
                return invRes;
            }
        }

        var paymentAmount = ResolveInitialPaymentAmount(
            template.PaymentModel,
            useInvoice,
            command.InitialPaymentAmount,
            registration.TotalAmount,
            minimumInitialAmountDue
        );
        return paymentAmount is null ? EventsRegistrationErrors.PaymentAmountInvalid
            : paymentAmount.Value <= 0m ? Result.Success()
            : await ChargeAndRecordAsync(
                registration,
                template,
                paymentAmount.Value,
                command.OneTimeHostedPaymentToken,
                command.SavedPaymentMethodId,
                externalTransactionCode: null,
                $"Initial registration payment ({registration.ConfirmationCode})",
                nowUtc,
                cancellationToken
            );
    }

    public Task<Result> ChargeAndRecordAsync(
        Registration registration,
        RegistrationTemplate template,
        decimal amount,
        string? oneTimeHostedToken,
        Guid? savedPaymentMethodId,
        string? externalTransactionCode,
        string? note,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        return amount <= 0m || amount > registration.BalanceDue
            ? Task.FromResult<Result>(EventsRegistrationErrors.PaymentAmountInvalid)
            : ExecuteTransactionAsync(
                registration,
                template,
                amount,
                oneTimeHostedToken,
                savedPaymentMethodId,
                externalTransactionCode,
                note,
                nowUtc,
                cancellationToken
            );
    }

    private async Task<Result> ExecuteTransactionAsync(
        Registration registration,
        RegistrationTemplate template,
        decimal amount,
        string? oneTimeHostedToken,
        Guid? savedPaymentMethodId,
        string? externalTransactionCode,
        string? note,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var hasElectronic =
            !string.IsNullOrWhiteSpace(oneTimeHostedToken) || savedPaymentMethodId.HasValue;
        var gatewayId = hasElectronic ? template.DefaultFinancialGatewayId : null;
        var txCode =
            !hasElectronic && string.IsNullOrWhiteSpace(externalTransactionCode)
                ? $"REGPAY-{registration.ConfirmationCode}"
                : externalTransactionCode;

        var txRes = await commandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                registration.RegistrarPersonId,
                registration.CurrencyCode,
                hasElectronic
                    ? FinancialTransactionSourceType.Website
                    : FinancialTransactionSourceType.StaffEntry,
                hasElectronic ? FinancialTenderType.CreditCard : FinancialTenderType.Check,
                [
                    new FinancialTransactionSplitSpec(
                        template.FinancialAccountId,
                        amount,
                        registration.LinkedInvoiceId,
                        note
                    ),
                ],
                FinancialGatewayId: gatewayId,
                SavedPaymentMethodId: savedPaymentMethodId,
                OneTimeHostedToken: oneTimeHostedToken,
                TransactionCode: txCode,
                TransactionDateTimeUtc: nowUtc,
                Summary: note
            ),
            cancellationToken
        );
        if (txRes.IsFailure)
        {
            return Result.Failure(txRes.Errors);
        }

        var rec = registration.RecordPayment(txRes.Value.Id, amount, nowUtc, note);
        return rec.IsFailure ? Result.Failure(rec.Errors) : Result.Success();
    }

    private async Task<Result> IssueRegistrationInvoiceAsync(
        Registration registration,
        RegistrationInstance instance,
        RegistrationTemplate template,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var billedPersonId = registration.RegistrarPersonId;
        if (billedPersonId is null || billedPersonId == Guid.Empty)
        {
            var personRes = await commandDispatcher.SendAsync<
                CreatePersonCommand,
                PersonDetailResponse
            >(
                new CreatePersonCommand(
                    registration.RegistrarFirstName,
                    registration.RegistrarLastName,
                    Email: registration.RegistrarEmail,
                    PrimaryBranchId: instance.BranchId
                ),
                cancellationToken
            );
            if (personRes.IsFailure)
            {
                return Result.Failure(personRes.Errors);
            }

            billedPersonId = personRes.Value.Id;
        }

        var issueDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var dueDate =
            instance.PaymentDeadlineDate is { } d && d >= issueDate ? d : issueDate.AddDays(30);

        var invRes = await commandDispatcher.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            new CreateInvoiceCommand(
                billedPersonId.Value,
                issueDate,
                dueDate,
                registration.CurrencyCode,
                [
                    new InvoiceLineItemSpec(
                        template.FinancialAccountId,
                        $"{instance.Name} ({registration.ConfirmationCode})",
                        Quantity: 1m,
                        registration.TotalAmount
                    ),
                ],
                IssueImmediately: true,
                BranchId: instance.BranchId,
                Memo: $"Event registration {registration.ConfirmationCode}"
            ),
            cancellationToken
        );
        if (invRes.IsFailure)
        {
            return Result.Failure(invRes.Errors);
        }

        registration.SetLinkedInvoice(invRes.Value.Id);
        return Result.Success();
    }

    private static decimal? ResolveInitialPaymentAmount(
        RegistrationPaymentModel model,
        bool useInvoice,
        decimal? suppliedAmount,
        decimal totalAmount,
        decimal minDeposit
    )
    {
        if (model == RegistrationPaymentModel.Free)
        {
            return 0m;
        }

        if (useInvoice && !suppliedAmount.HasValue)
        {
            return 0m;
        }

        var effective = decimal.Round(
            suppliedAmount
                ?? (model == RegistrationPaymentModel.PayInFull ? totalAmount : minDeposit),
            2,
            MidpointRounding.AwayFromZero
        );

        return
            effective > totalAmount
            || effective < 0m
            || (
                !useInvoice
                && model == RegistrationPaymentModel.PayInFull
                && effective < totalAmount
            )
            || (!useInvoice && effective < minDeposit)
            ? null
            : effective;
    }
}
