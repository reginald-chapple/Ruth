using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.CheckIn.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

[Collection(CheckInPostgresFixtures.Name)]
public sealed class W7S5CheckInMultiTenantRlsIntegrationTests(
    CheckInPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_SystemLabels_CustomLabels_AndPickupCodeLedger_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CheckInTestHostFactory.BuildAndMigrateAsync(dbConn);
        await AssertAllCheckInTablesHaveForceRlsAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var sessionA = await SeedLabelsAndSessionAsync(
            provider,
            tenantA,
            "alpha-checkin",
            "Alpha Claim Tag"
        );
        var sessionB = await SeedLabelsAndSessionAsync(
            provider,
            tenantB,
            "beta-checkin",
            "Beta Claim Tag"
        );

        await VerifyCrossTenantCheckInIsolationAsync(
            provider,
            tenantA,
            sessionA,
            tenantB,
            sessionB
        );
    }

    private static async Task<Guid> SeedLabelsAndSessionAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string labelName
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<CheckInTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantId);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var label = await commands.SendAsync<CreateCheckInLabelCommand, CheckInLabelResponse>(
                new CreateCheckInLabelCommand(
                    labelName,
                    "food-box-claim-tag",
                    CheckInLabelKind.VisitorNameBadge,
                    CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto()
                )
            );
            Assert.True(label.IsSuccess);

            var scheduleId = Guid.CreateVersion7();
            var roomId = Guid.CreateVersion7();
            var (configId, areaId) =
                await W3S3CheckInProfileSeedHelper.ConfigureCheckInProfileAsync(
                    commands,
                    linkedGroupId: Guid.Empty,
                    scheduleId,
                    roomId,
                    Guid.CreateVersion7()
                );

            var session = await commands.SendAsync<
                SubmitCheckInSessionCommand,
                CheckInSessionReceiptResponse
            >(
                new SubmitCheckInSessionCommand(
                    configId,
                    scheduleId,
                    [
                        new CheckInSelectionItemDto(
                            Guid.CreateVersion7(),
                            areaId,
                            roomId,
                            scheduleId,
                            PersonFullName: "Liam Rivera",
                            ParticipantContext: new CheckInParticipantContextDto(
                                AgeYears: 8m,
                                MembershipTier: "Family"
                            )
                        ),
                    ]
                )
            );
            Assert.True(session.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(session.Value.SecurityPickupCode));
            return session.Value.SessionId;
        }
    }

    private static async Task VerifyCrossTenantCheckInIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid sessionA,
        TenantId tenantB,
        Guid sessionB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-checkin", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var labelsB = (
                await queries.QueryAsync<
                    GetCheckInLabelsQuery,
                    IReadOnlyList<CheckInLabelResponse>
                >(new GetCheckInLabelsQuery(ActiveOnly: false))
            ).Value;
            Assert.Equal(3, labelsB.Count);
            Assert.Contains(labelsB, static l =>
                string.Equals(l.Name, "Beta Claim Tag", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(labelsB, static l =>
                string.Equals(l.Name, "Alpha Claim Tag", StringComparison.Ordinal)
            );

            var db = scope.ServiceProvider.GetRequiredService<CheckInDbContext>();
            Assert.Equal(1, await db.DailySecurityPickupCodes.CountAsync());
            var sessions = await db
                .AttendanceCheckInSessions.Select(static s => s.Id.Value)
                .ToListAsync();
            var singleB = Assert.Single(sessions);
            Assert.Equal(sessionB, singleB);
            Assert.DoesNotContain(sessionA, sessions);
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-checkin", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CheckInDbContext>();
            Assert.Equal(1, await db.DailySecurityPickupCodes.CountAsync());
            var sessions = await db
                .AttendanceCheckInSessions.Select(static s => s.Id.Value)
                .ToListAsync();
            var singleA = Assert.Single(sessions);
            Assert.Equal(sessionA, singleA);
        }
    }

    private static async Task AssertAllCheckInTablesHaveForceRlsAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = 'checkin' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Assert.True(reader.GetBoolean(1), $"Missing RLS on checkin.{reader.GetString(0)}");
            Assert.True(
                reader.GetBoolean(2),
                $"Missing FORCE RLS on checkin.{reader.GetString(0)}"
            );
        }

        Assert.True(count >= 8);
    }
}
