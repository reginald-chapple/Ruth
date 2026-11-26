using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S4PersonMergedConsumerIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task FinancePersonMergedConsumer_RepointsAllSevenConstituentEntities()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var (accountId, gatewayId, methodId, retiredId) =
            await W4S4RecurringWebhooksCadenceAndMergeIntegrationTests.SeedGatewayAndMethodAsync(
                commands,
                queries
            );
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        await SeedRetiredPledgeBankAndBillingAsync(commands, retiredId, accountId, today);
        await SeedRetiredRecurringAndAlertAsync(
            commands,
            retiredId,
            gatewayId,
            methodId,
            accountId,
            today
        );

        var survivingId = Guid.CreateVersion7();
        var consumer = scope.ServiceProvider.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingId,
                retiredId,
                [],
                HadMultipleDistinctEmails: false
            )
        );

        await AssertSurvivingCountsAsync(dbContext, survivingId);
    }

    private static async Task SeedRetiredPledgeBankAndBillingAsync(
        ICommandDispatcher commands,
        Guid retiredId,
        Guid accountId,
        DateOnly today
    )
    {
        _ = await commands.SendAsync<CreateFinancialPledgeCommand, FinancialPledgeResponse>(
            new CreateFinancialPledgeCommand(
                retiredId,
                accountId,
                500.00m,
                "USD",
                PledgeFrequency.Monthly,
                today,
                today.AddMonths(6)
            )
        );
        _ = await commands.SendAsync<
            RegisterBankAccountIdentifierCommand,
            BankAccountIdentifierResponse
        >(new RegisterBankAccountIdentifierCommand(retiredId, "021000021", "5566778899"));
        _ = await commands.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            new CreateInvoiceCommand(
                retiredId,
                today,
                today.AddDays(15),
                "USD",
                [new InvoiceLineItemSpec(accountId, "Fee", 1m, 45.00m)]
            )
        );
        _ = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                retiredId,
                "USD",
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Cash,
                [new FinancialTransactionSplitSpec(accountId, 60.00m)]
            )
        );
    }

    private static async Task SeedRetiredRecurringAndAlertAsync(
        ICommandDispatcher commands,
        Guid retiredId,
        Guid gatewayId,
        Guid methodId,
        Guid accountId,
        DateOnly today
    )
    {
        var schedule = (
            await commands.SendAsync<CreateRecurringScheduleCommand, RecurringScheduleResponse>(
                new CreateRecurringScheduleCommand(
                    retiredId,
                    gatewayId,
                    methodId,
                    RecurringSchedulePurpose.MembershipSubscription,
                    PledgeFrequency.Monthly,
                    "USD",
                    today.AddMonths(1),
                    [new RecurringScheduleSplitSpec(accountId, 50.00m)]
                )
            )
        ).Value;

        await W4S4RecurringWebhooksCadenceAndMergeIntegrationTests.TriggerPastDueAlertAsync(
            commands,
            gatewayId,
            schedule.GatewaySubscriptionReference!,
            today
        );
    }

    private static async Task AssertSurvivingCountsAsync(
        FinanceDbContext dbContext,
        Guid survivingId
    )
    {
        Assert.Equal(
            1,
            await dbContext.FinancialPledges.CountAsync(p => p.PersonId == survivingId)
        );
        Assert.Equal(
            1,
            await dbContext.SavedPaymentMethods.CountAsync(m => m.PersonId == survivingId)
        );
        Assert.Equal(
            1,
            await dbContext.BankAccountIdentifiers.CountAsync(b => b.PersonId == survivingId)
        );
        Assert.Equal(1, await dbContext.Invoices.CountAsync(i => i.PersonId == survivingId));
        Assert.Equal(
            1,
            await dbContext.FinancialTransactions.CountAsync(t =>
                t.AuthorizedPersonId == survivingId
            )
        );
        Assert.Equal(
            1,
            await dbContext.RecurringSchedules.CountAsync(s => s.PersonId == survivingId)
        );
        Assert.True(await dbContext.CadenceAlerts.AnyAsync(a => a.PersonId == survivingId));
    }
}
