using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class W4S9CommerceVerificationHelper
{
    public static async Task VerifyDigitalLinkWalletPassSyncAndRefundAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CommerceSeedBundle seed,
        CommerceOrderResponse order
    )
    {
        var digitalLine = order.Lines.Single(static l =>
            l.ProductKind == ProductKind.DigitalDownload
        );
        var dlRes = await queries.QueryAsync<
            GetOrderDigitalDownloadLinkQuery,
            OrderDigitalDownloadLinkResponse
        >(new GetOrderDigitalDownloadLinkQuery(order.OrderNumber, digitalLine.LineId));
        Assert.True(dlRes.IsSuccess);
        Assert.Contains(
            "binary-files",
            dlRes.Value.DownloadUrl,
            StringComparison.OrdinalIgnoreCase
        );

        var walletPasses = (
            await queries.QueryAsync<
                GetIssuedWalletPassesQuery,
                IReadOnlyList<IssuedWalletPassResponse>
            >(new GetIssuedWalletPassesQuery(PersonId: seed.MemberPersonId, ActiveOnly: true))
        ).Value;
        Assert.Equal(10, walletPasses[0].DynamicFields.PunchOrCreditBalance);

        var issuedPassId = Assert.Single(order.IssuedPunchPassIds);
        var redeemed = await commands.SendAsync<
            RedeemPunchPassCreditsCommand,
            IssuedPunchPassResponse
        >(
            new RedeemPunchPassCreditsCommand(
                issuedPassId,
                CreditsToRedeem: 3,
                RedeemedForPersonId: seed.HouseholdPartnerPersonId,
                ReferenceNotes: "Partner household workout"
            )
        );
        Assert.True(redeemed.IsSuccess);
        Assert.Equal(7, redeemed.Value.RemainingCredits);

        var walletAfterRedeem = (
            await queries.QueryAsync<
                GetIssuedWalletPassesQuery,
                IReadOnlyList<IssuedWalletPassResponse>
            >(new GetIssuedWalletPassesQuery(PersonId: seed.MemberPersonId, ActiveOnly: true))
        ).Value;
        Assert.Equal(7, walletAfterRedeem[0].DynamicFields.PunchOrCreditBalance);

        var shipped = await commands.SendAsync<
            UpdateCommerceOrderFulfillmentCommand,
            CommerceOrderResponse
        >(
            new UpdateCommerceOrderFulfillmentCommand(
                order.Id,
                CommerceOrderStatus.Shipped,
                "USPS-94001"
            )
        );
        Assert.True(shipped.IsSuccess);

        var refunded = await commands.SendAsync<RefundCommerceOrderCommand, CommerceOrderResponse>(
            new RefundCommerceOrderCommand(
                order.Id,
                order.TotalAmount,
                "Full order return",
                RestockInventory: true
            )
        );
        Assert.True(refunded.IsSuccess);
        Assert.Equal(CommerceOrderStatus.Refunded, refunded.Value.Status);

        var walletAfterRefund = (
            await queries.QueryAsync<
                GetIssuedWalletPassesQuery,
                IReadOnlyList<IssuedWalletPassResponse>
            >(new GetIssuedWalletPassesQuery(PersonId: seed.MemberPersonId, ActiveOnly: true))
        ).Value;
        Assert.Equal(0, walletAfterRefund[0].DynamicFields.PunchOrCreditBalance);
    }

    public static async Task VerifyInvoiceCheckoutAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CommerceSeedBundle seed,
        Guid qpqVariantId
    )
    {
        var invOrderRes = await commands.SendAsync<
            SubmitCommerceOrderCommand,
            CommerceOrderResponse
        >(
            new SubmitCommerceOrderCommand(
                "Nora Chen",
                "nora.chen@example.org",
                [new CommerceCartItemDto(qpqVariantId, 1)],
                BuyerPersonId: seed.MemberPersonId,
                FulfillmentMethod: CommerceFulfillmentMethod.LocalPickup,
                RequestInvoiceBilling: true
            )
        );
        Assert.True(invOrderRes.IsSuccess);
        Assert.Equal(CommerceOrderStatus.PendingPayment, invOrderRes.Value.Status);
        var linkedInvoiceId = Assert.NotNull(invOrderRes.Value.LinkedInvoiceId);

        var invoice = (
            await queries.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
                new GetInvoiceByIdQuery(linkedInvoiceId)
            )
        ).Value;
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(2, invoice.LineItems.Count);
    }
}
