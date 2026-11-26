using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S4RecurringWebhooksCadenceAndMergeIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    private const string WebhookSecret = "ruth-dev-test-gateway-webhook-secret-2026";

    [Fact]
    public async Task RecurringSettlementSync_IdempotentWebhooks_AndCadenceAlerts_WorkEndToEnd()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (accountId, gatewayId, methodId, personId) = await SeedGatewayAndMethodAsync(
            commands,
            queries
        );
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        var schedule = (
            await commands.SendAsync<CreateRecurringScheduleCommand, RecurringScheduleResponse>(
                new CreateRecurringScheduleCommand(
                    personId,
                    gatewayId,
                    methodId,
                    RecurringSchedulePurpose.CharitableContribution,
                    PledgeFrequency.Monthly,
                    "USD",
                    today,
                    [new RecurringScheduleSplitSpec(accountId, 100.00m, "Monthly Sustainer")]
                )
            )
        ).Value;

        var syncRun = (
            await commands.SendAsync<
                ProcessDueRecurringSchedulesCommand,
                RecurringSettlementRunResponse
            >(new ProcessDueRecurringSchedulesCommand(AsOfDate: today))
        ).Value;
        Assert.Equal(1, syncRun.SettledCount);

        await VerifyWebhookIdempotencyAndDunningAsync(
            commands,
            queries,
            gatewayId,
            schedule.GatewaySubscriptionReference!,
            personId,
            today
        );
    }

    internal static async Task<(
        Guid AccountId,
        Guid GatewayId,
        Guid MethodId,
        Guid PersonId
    )> SeedGatewayAndMethodAsync(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        var accountId = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery())
        )
            .Value[0]
            .Id;
        var gatewayId = (
            await queries.QueryAsync<
                GetFinancialGatewaysQuery,
                IReadOnlyList<FinancialGatewayResponse>
            >(new GetFinancialGatewaysQuery(ActiveOnly: true))
        )
            .Value[0]
            .Id;
        var personId = Guid.CreateVersion7();
        var method = (
            await commands.SendAsync<VaultSavedPaymentMethodCommand, SavedPaymentMethodResponse>(
                new VaultSavedPaymentMethodCommand(
                    personId,
                    gatewayId,
                    "tok_card_ok",
                    PaymentMethodKind.CreditCard,
                    MaskedLast4Hint: "4242",
                    ExpiryMonthHint: 12,
                    ExpiryYearHint: 2030
                )
            )
        ).Value;
        return (accountId, gatewayId, method.Id, personId);
    }

    internal static async Task TriggerPastDueAlertAsync(
        ICommandDispatcher commands,
        Guid gatewayId,
        string subRef,
        DateOnly today
    )
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var attemptText = attempt.ToString(CultureInfo.InvariantCulture);
            var failBody =
                $$"""{"eventId":"wh_fail_{{subRef}}_{{attemptText}}","eventKind":"SubscriptionChargeFailed","subscriptionReference":"{{subRef}}","failureMessage":"Declined"}""";
            _ = await commands.SendAsync<
                ProcessGatewayWebhookCommand,
                GatewayWebhookProcessResponse
            >(new ProcessGatewayWebhookCommand(gatewayId, failBody, SignTestWebhook(failBody)));
        }

        var scan = await commands.SendAsync<
            RunTransactionCadenceScanCommand,
            CadenceScanSummaryResponse
        >(new RunTransactionCadenceScanCommand(today));
        Assert.True(scan.IsSuccess);
        Assert.True(scan.Value.AlertsCreatedCount >= 1);
    }

    private static async Task VerifyWebhookIdempotencyAndDunningAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid gatewayId,
        string subRef,
        Guid personId,
        DateOnly today
    )
    {
        var body =
            $$"""{"eventId":"wh_ok_1","eventKind":"SubscriptionChargeSucceeded","subscriptionReference":"{{subRef}}","transactionId":"gtx_99","amount":100.00,"currencyCode":"USD"}""";
        var sig = SignTestWebhook(body);

        var first = (
            await commands.SendAsync<ProcessGatewayWebhookCommand, GatewayWebhookProcessResponse>(
                new ProcessGatewayWebhookCommand(gatewayId, body, sig)
            )
        ).Value;
        Assert.False(first.WasDuplicate);
        _ = Assert.NotNull(first.RecordedTransactionId);

        var duplicate = (
            await commands.SendAsync<ProcessGatewayWebhookCommand, GatewayWebhookProcessResponse>(
                new ProcessGatewayWebhookCommand(gatewayId, body, sig)
            )
        ).Value;
        Assert.True(duplicate.WasDuplicate);

        await TriggerPastDueAlertAsync(commands, gatewayId, subRef, today);

        var secondScan = (
            await commands.SendAsync<RunTransactionCadenceScanCommand, CadenceScanSummaryResponse>(
                new RunTransactionCadenceScanCommand(today)
            )
        ).Value;
        Assert.Equal(0, secondScan.AlertsCreatedCount);

        var alerts = (
            await queries.QueryAsync<
                GetTransactionCadenceAlertsQuery,
                IReadOnlyList<TransactionCadenceAlertResponse>
            >(new GetTransactionCadenceAlertsQuery(PersonId: personId))
        ).Value;
        Assert.NotEmpty(alerts);

        var resolved = await commands.SendAsync<
            UpdateCadenceAlertStatusCommand,
            TransactionCadenceAlertResponse
        >(
            new UpdateCadenceAlertStatusCommand(
                alerts[0].Id,
                CadenceAlertStatus.Resolved,
                "Updated card on file"
            )
        );
        Assert.True(resolved.IsSuccess);
        Assert.Equal(CadenceAlertStatus.Resolved, resolved.Value.Status);
    }

    private static string SignTestWebhook(string rawBody)
    {
        var mac = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(WebhookSecret),
            Encoding.UTF8.GetBytes(rawBody)
        );
        return $"sha256={Convert.ToHexStringLower(mac)}";
    }
}
