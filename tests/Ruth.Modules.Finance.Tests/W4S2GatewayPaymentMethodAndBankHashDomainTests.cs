using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S2GatewayPaymentMethodAndBankHashDomainTests
{
    [Fact]
    public void FinancialGateway_AndSavedPaymentMethod_EnforceTokenOnlyAndProviderInvariants()
    {
        Assert.True(FinancialGateway.Create(new FinancialGatewaySpec("", "test")).IsFailure);
        Assert.True(
            FinancialGateway.Create(new FinancialGatewaySpec("Bad Key", "BAD KEY!")).IsFailure
        );
        Assert.True(
            FinancialGateway
                .Create(new FinancialGatewaySpec("Bad Offset", "test", BatchTimeOffsetHours: 20))
                .IsFailure
        );

        var gateway = FinancialGateway
            .Create(
                new FinancialGatewaySpec(
                    "Primary Stripe",
                    "STRIPE",
                    SupportsCards: true,
                    SupportsAch: true,
                    SupportsRecurring: true,
                    IsTestMode: true,
                    BatchTimeOffsetHours: -5,
                    PublicTokenizerKey: "pk_test_123",
                    Settings: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["statement_descriptor"] = "RUTH ORG",
                    }
                )
            )
            .Value;
        Assert.Equal("stripe", gateway.ProviderKey);

        var personId = Guid.CreateVersion7();
        Assert.True(
            SavedPaymentMethod
                .Create(
                    personId,
                    gateway.Id,
                    "4111111111111111",
                    PaymentMethodKind.CreditCard,
                    "Raw PAN Attempt",
                    "Visa",
                    "****1111",
                    12,
                    2029
                )
                .IsFailure
        );
        Assert.True(
            SavedPaymentMethod
                .Create(
                    personId,
                    gateway.Id,
                    "vault_tok_safe",
                    PaymentMethodKind.CreditCard,
                    "Invalid Expiry",
                    "Visa",
                    "****4242",
                    13,
                    2029
                )
                .IsFailure
        );

        var saved = SavedPaymentMethod
            .Create(
                personId,
                gateway.Id,
                "vault_tok_safe",
                PaymentMethodKind.CreditCard,
                displayName: null,
                cardBrand: "Visa",
                accountNumberMasked: "4242",
                expiryMonth: 11,
                expiryYear: 2029,
                isDefault: true
            )
            .Value;
        Assert.Equal("****4242", saved.AccountNumberMasked);
        Assert.Equal("Visa ****4242", saved.DisplayName);
        saved.Deactivate();
        Assert.False(saved.IsActive);
        Assert.False(saved.IsDefault);
    }

    [Fact]
    public void AbaRoutingChecksum_AndBankAccountHasher_ValidateAndHashDeterministically()
    {
        Assert.True(AbaRoutingChecksumValidator.TryNormalizeAndValidate("0210-0002-1", out var r1));
        Assert.Equal("021000021", r1);
        Assert.True(AbaRoutingChecksumValidator.TryNormalizeAndValidate("011000015", out _));
        Assert.False(AbaRoutingChecksumValidator.TryNormalizeAndValidate("021000022", out _));
        Assert.False(AbaRoutingChecksumValidator.TryNormalizeAndValidate("000000000", out _));
        Assert.False(AbaRoutingChecksumValidator.TryNormalizeAndValidate("12345", out _));

        var key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
        var first = BankAccountIdentifierHasher.ComputeDigest("021000021", "9876-54321", key);
        var second = BankAccountIdentifierHasher.ComputeDigest("0210-0002-1", "987654321", key);
        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value.HashHex, second.Value.HashHex);
        Assert.Equal("*****0021", first.Value.MaskedRouting);
        Assert.Equal("****4321", first.Value.MaskedAccount);

        Assert.True(
            BankAccountIdentifierHasher.ComputeDigest("021000022", "987654321", key).IsFailure
        );
        Assert.True(BankAccountIdentifierHasher.ComputeDigest("021000021", "12", key).IsFailure);
    }

    [Fact]
    public async Task NmiAndStripeProviders_VaultChargeRefundSyncAndVerifyHmacWebhooks()
    {
        var personId = Guid.CreateVersion7();
        var nmiProvider = GatewayProviderTestHttpFactory.CreateNmiProvider();
        var nmiVault = await nmiProvider.VaultPaymentMethodAsync(
            new GatewayVaultRequest(
                personId,
                "tok_nmi",
                PaymentMethodKind.CreditCard,
                BillingName: "Donor",
                BillingEmail: "donor@ruth.test",
                MaskedLast4Hint: "1111",
                CardBrandHint: "Visa",
                ExpiryMonthHint: 12,
                ExpiryYearHint: 2029,
                IsTestMode: true
            )
        );
        Assert.True(nmiVault.IsSuccess);
        Assert.Equal("nvault_1", nmiVault.Value.CustomerVaultToken);

        var nmiCharge = await nmiProvider.ChargeAsync(
            new GatewayChargeRequest(
                50.00m,
                "USD",
                "nvault_1",
                OneTimeHostedToken: null,
                PaymentMethodKind.CreditCard,
                "idem-1",
                "Gift",
                IsTestMode: true
            )
        );
        Assert.True(nmiCharge.IsSuccess);
        Assert.True(
            (
                await nmiProvider.RefundAsync(
                    new GatewayRefundRequest("ntx_1", 50.00m, "USD", "Refund", IsTestMode: true)
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await nmiProvider.SyncSubscriptionAsync(
                    new GatewaySubscriptionSyncRequest(
                        Guid.CreateVersion7(),
                        "nvault_1",
                        50m,
                        "USD",
                        PledgeFrequency.Monthly,
                        new DateOnly(2026, 5, 1),
                        GatewaySubscriptionAction.CreateOrUpdate,
                        IsTestMode: true
                    )
                )
            ).IsSuccess
        );

        var nmiBody = JsonSerializer.Serialize(
            new
            {
                event_id = "ev_nmi_1",
                event_type = "transaction.sale.success",
                event_body = new
                {
                    subscription_id = "nsub_1",
                    transaction_id = "ntx_2",
                    amount = "50.00",
                },
            }
        );
        var nmiTs = DateTimeOffset
            .UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var nmiSig = ComputeHmacHex("nmi-secret", $"{nmiTs}.{nmiBody}");
        Assert.True(nmiProvider.ParseAndVerifyWebhook(nmiBody, $"t={nmiTs},s={nmiSig}").IsSuccess);
        Assert.True(nmiProvider.ParseAndVerifyWebhook(nmiBody, $"t={nmiTs},s=deadbeef").IsFailure);
        var staleNmiSig = ComputeHmacHex("nmi-secret", $"999.{nmiBody}");
        Assert.True(nmiProvider.ParseAndVerifyWebhook(nmiBody, $"t=999,s={staleNmiSig}").IsFailure);

        await VerifyStripeAdapterAsync(personId);
    }

    private static async Task VerifyStripeAdapterAsync(Guid personId)
    {
        var stripeProvider = GatewayProviderTestHttpFactory.CreateStripeProvider();
        var stripeVault = await stripeProvider.VaultPaymentMethodAsync(
            new GatewayVaultRequest(
                personId,
                "pm_card_visa",
                PaymentMethodKind.CreditCard,
                BillingName: "Jane",
                BillingEmail: "jane@ruth.test",
                MaskedLast4Hint: "4242",
                CardBrandHint: "Visa",
                ExpiryMonthHint: 8,
                ExpiryYearHint: 2030,
                IsTestMode: true
            )
        );
        Assert.True(stripeVault.IsSuccess);
        Assert.True(
            (
                await stripeProvider.ChargeAsync(
                    new GatewayChargeRequest(
                        75.00m,
                        "USD",
                        stripeVault.Value.CustomerVaultToken,
                        OneTimeHostedToken: null,
                        PaymentMethodKind.CreditCard,
                        "idem-2",
                        "Dues",
                        IsTestMode: true
                    )
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await stripeProvider.RefundAsync(
                    new GatewayRefundRequest("pi_123", 25.00m, "USD", "Partial", IsTestMode: true)
                )
            ).IsSuccess
        );

        var stripeBody = JsonSerializer.Serialize(
            new
            {
                id = "evt_1",
                type = "invoice.payment_succeeded",
                data = new
                {
                    @object = new
                    {
                        subscription = "sub_1",
                        payment_intent = "pi_9",
                        amount_paid = 7500,
                    },
                },
            }
        );
        var stripeTs = DateTimeOffset
            .UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var stripeSig = ComputeHmacHex("whsec_test", $"{stripeTs}.{stripeBody}");
        Assert.True(
            stripeProvider
                .ParseAndVerifyWebhook(stripeBody, $"t={stripeTs},v1={stripeSig}")
                .IsSuccess
        );
        Assert.True(
            stripeProvider.ParseAndVerifyWebhook(stripeBody, $"t={stripeTs},v1=0000").IsFailure
        );
        var staleStripeSig = ComputeHmacHex("whsec_test", $"1700000000.{stripeBody}");
        Assert.True(
            stripeProvider
                .ParseAndVerifyWebhook(stripeBody, $"t=1700000000,v1={staleStripeSig}")
                .IsFailure
        );
    }

    private static string ComputeHmacHex(string key, string payload)
    {
        return Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(payload))
        );
    }
}
