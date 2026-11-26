using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Tests.Fixtures;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

[Collection(CommercePostgresFixtures.Name)]
public sealed class W4S10BookingsIntegrationTests(CommercePostgresContainerFixture postgres)
{
    [Fact]
    public async Task ConflictFreeSlots_CoreSchedules_TtlHolds_VirtualBookingOutbox_WalletSync_Cancellation_AndPersonMerge_WorkEndToEnd()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CommerceTestHostFactory.BuildAndMigrateAsync(dbConn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var seed = await CommerceTestSeedHelper.SeedAsync(commands, queries);

        var (service, resource, slotStartUtc) =
            await W4S10BookingsSetupHelper.SetupServiceResourceAndSchedulesAsync(commands, seed);

        var confirmedServiceBooking =
            await W4S10BookingsVerificationHelper.VerifySlotHoldsMemberFreeBookingOutboxAndWalletAsync(
                commands,
                queries,
                dbContext,
                seed,
                service,
                slotStartUtc
            );

        await W4S10BookingsVerificationHelper.VerifyResourceRentalPunchPassAndCancellationAsync(
            commands,
            queries,
            seed,
            service,
            resource,
            slotStartUtc,
            confirmedServiceBooking.Id
        );

        await W4S10BookingsVerificationHelper.VerifyHoldCleanupJobAndPersonMergeAsync(
            provider,
            dbContext,
            seed,
            confirmedServiceBooking.Id,
            JobExecutionStatus.Success
        );
    }
}
