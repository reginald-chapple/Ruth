using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class BookingSettlementCollaborator(
    CommerceOrderFinanceCollaborator orderFinance,
    ICommandDispatcher commandDispatcher
)
{
    public async Task<Result> SettleBookingAsync(
        ServiceOrResourceBooking booking,
        BookingQuoteResponse quote,
        Guid? revenueAccountId,
        Guid? defaultGatewayId,
        ConfirmBookingCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (quote.PricingKind == BookingPricingKind.PunchPassRedeemed)
        {
            if (command.RedeemPunchPassId is not { } passId || passId == Guid.Empty)
            {
                return CommerceErrors.BookingPaymentInvalid;
            }

            var redeemRes = await commandDispatcher.SendAsync<
                RedeemPunchPassCreditsCommand,
                IssuedPunchPassResponse
            >(
                new RedeemPunchPassCreditsCommand(
                    passId,
                    quote.PunchPassCreditsRequired,
                    booking.CustomerPersonId,
                    booking.BranchId,
                    $"Booking {booking.ConfirmationCode} ({booking.TargetName})"
                ),
                cancellationToken
            );
            if (redeemRes.IsFailure)
            {
                return Result.Failure(redeemRes.Errors);
            }

            booking.AttachPunchPass(IssuedPunchPassId.From(passId));
            return Result.Success();
        }

        return quote.TotalAmount <= 0m ? Result.Success()
            : revenueAccountId is not { } accountId || accountId == Guid.Empty
                ? (Result)CommerceErrors.BookingPaymentInvalid
            : command.RequestInvoiceBilling
                ? !quote.AllowInvoiceBilling ? CommerceErrors.BookingPaymentInvalid
                    : await IssueBookingInvoiceAsync(booking, accountId, nowUtc, cancellationToken)
            : await ChargeBookingAsync(
                booking,
                accountId,
                defaultGatewayId,
                command,
                nowUtc,
                cancellationToken
            );
    }

    public async Task<Result> RefundOrVoidOnCancelAsync(
        ServiceOrResourceBooking booking,
        string reason,
        Guid? processedByPersonId,
        CancellationToken cancellationToken
    )
    {
        if (booking.LinkedFinancialTransactionId is { } txId && booking.PaidAmount > 0m)
        {
            var refRes = await commandDispatcher.SendAsync<
                RefundFinancialTransactionCommand,
                FinancialTransactionResponse
            >(
                new RefundFinancialTransactionCommand(
                    txId,
                    booking.PaidAmount,
                    $"Cancelled booking {booking.ConfirmationCode}: {reason}",
                    processedByPersonId
                ),
                cancellationToken
            );
            return refRes.IsFailure ? Result.Failure(refRes.Errors) : Result.Success();
        }

        if (booking.LinkedInvoiceId is { } invId)
        {
            var voidRes = await commandDispatcher.SendAsync<VoidInvoiceCommand, InvoiceResponse>(
                new VoidInvoiceCommand(
                    invId,
                    $"Cancelled booking {booking.ConfirmationCode}: {reason}"
                ),
                cancellationToken
            );
            return voidRes.IsFailure ? Result.Failure(voidRes.Errors) : Result.Success();
        }

        return Result.Success();
    }

    private async Task<Result> IssueBookingInvoiceAsync(
        ServiceOrResourceBooking booking,
        Guid accountId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var personRes = await orderFinance.EnsureConstituentPersonAsync(
            booking.CustomerPersonId,
            booking.CustomerName,
            booking.CustomerEmail,
            booking.BranchId,
            cancellationToken
        );
        if (personRes.IsFailure)
        {
            return Result.Failure(personRes.Errors);
        }

        var issueDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var invRes = await commandDispatcher.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            new CreateInvoiceCommand(
                personRes.Value,
                issueDate,
                issueDate.AddDays(14),
                booking.CurrencyCode,
                [
                    new InvoiceLineItemSpec(
                        accountId,
                        $"{booking.TargetName} ({booking.ConfirmationCode})",
                        1m,
                        booking.TotalAmount
                    ),
                ],
                IssueImmediately: true,
                BranchId: booking.BranchId,
                Memo: $"Booking {booking.ConfirmationCode}"
            ),
            cancellationToken
        );
        if (invRes.IsFailure)
        {
            return Result.Failure(invRes.Errors);
        }

        booking.AttachInvoice(invRes.Value.Id);
        return Result.Success();
    }

    private async Task<Result> ChargeBookingAsync(
        ServiceOrResourceBooking booking,
        Guid accountId,
        Guid? defaultGatewayId,
        ConfirmBookingCommand command,
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
            return CommerceErrors.BookingPaymentInvalid;
        }

        var txRes = await commandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                booking.CustomerPersonId,
                booking.CurrencyCode,
                hasElectronic
                    ? FinancialTransactionSourceType.Website
                    : FinancialTransactionSourceType.StaffEntry,
                hasElectronic ? FinancialTenderType.CreditCard : FinancialTenderType.Check,
                [
                    new FinancialTransactionSplitSpec(
                        accountId,
                        booking.TotalAmount,
                        InvoiceId: null,
                        Summary: $"{booking.TargetName} ({booking.ConfirmationCode})"
                    ),
                ],
                FinancialGatewayId: hasElectronic
                    ? command.FinancialGatewayId ?? defaultGatewayId
                    : null,
                SavedPaymentMethodId: command.SavedPaymentMethodId,
                OneTimeHostedToken: command.OneTimeHostedPaymentToken,
                TransactionCode: command.ExternalTransactionCode,
                TransactionDateTimeUtc: nowUtc,
                Summary: $"Booking {booking.ConfirmationCode}"
            ),
            cancellationToken
        );
        if (txRes.IsFailure)
        {
            return Result.Failure(txRes.Errors);
        }

        booking.AttachPayment(txRes.Value.Id, txRes.Value.TotalAmount);
        return Result.Success();
    }
}
