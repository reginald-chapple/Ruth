using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Infrastructure;

[Collection(IdentityPostgresFixtures.Name)]
public sealed class W7S5IdentityMultiTenantRlsIntegrationTests(Postgres18ContainerFixture postgres)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_DeterministicSystemRoles_AndAclCache_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            dbConn,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);
        await AssertAllIdentityTablesHaveForceRlsAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        await SeedAndOnboardTenantAsync(provider, tenantA, "alpha-aid", "Alpha Aid Foundation");
        await SeedAndOnboardTenantAsync(provider, tenantB, "beta-relief", "Beta Relief Fund");

        await VerifyCrossTenantIsolationAndAclCacheAsync(provider, tenantA, tenantB);
    }

    [Fact]
    public void SystemRole_Update_BlocksDeactivationAndRenaming_AllowsDescriptionUpdate()
    {
        var adminRole = SecurityRole.CreateSystemAdministrators(TenantId.Create());

        var renameAttempt = adminRole.Update("SuperAdmins", "Renamed", isActive: true);
        Assert.True(renameAttempt.IsFailure);
        Assert.Equal("Identity.SystemRoleProtected", renameAttempt.PrimaryError.Code);

        var deactivateAttempt = adminRole.Update(
            SecurityRole.AdministratorsRoleName,
            "Disabled",
            isActive: false
        );
        Assert.True(deactivateAttempt.IsFailure);
        Assert.Equal("Identity.SystemRoleProtected", deactivateAttempt.PrimaryError.Code);

        var descUpdate = adminRole.Update(
            SecurityRole.AdministratorsRoleName,
            "Updated system administrators description.",
            isActive: true
        );
        Assert.True(descUpdate.IsSuccess);
        Assert.Equal("Updated system administrators description.", adminRole.Description);
    }

    private static async Task SeedAndOnboardTenantAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string orgName
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IdentityTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantId);

            var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var cmd = IdentityTestHostFactory.CreateValidCommand(
                userName: "orgadmin",
                email: "director@nonprofit.org"
            ) with
            {
                OrganizationName = orgName,
            };
            var onboard = await dispatcher.SendAsync<
                Contracts.Onboarding.CompleteOnboardingCommand,
                Contracts.Onboarding.CompleteOnboardingResponse
            >(cmd);
            Assert.True(onboard.IsSuccess);
        }
    }

    private static async Task VerifyCrossTenantIsolationAndAclCacheAsync(
        ServiceProvider provider,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var roleIdA = SecurityRole.GetAdministratorsRoleId(tenantA);
        var roleIdB = SecurityRole.GetAdministratorsRoleId(tenantB);
        Assert.NotEqual(roleIdA, roleIdB);

        var cache = provider.GetRequiredService<IAclSnapshotCache>();
        Guid customRoleIdA;

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-aid", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            Assert.Equal(1, await db.UserAccounts.CountAsync());
            Assert.Equal(
                "Alpha Aid Foundation",
                (await db.OrganizationProfiles.SingleAsync()).Name
            );

            var customRole = SecurityRole.Create(SecurityRoleId.Create(), "Caseworkers").Value;
            customRoleIdA = customRole.Id.Value;
            _ = db.SecurityRoles.Add(customRole);
            _ = db.EntityAclRules.Add(
                EntityAclRule
                    .Create(
                        "Ruth.Crm.Person",
                        Guid.Empty,
                        AuthorizationActions.Edit,
                        order: 1,
                        AclRuleDecision.Allow,
                        SpecialRoleType.None,
                        customRole.Id
                    )
                    .Value
            );
            _ = await db.SaveChangesAsync();
            await cache.InvalidateAsync();

            var snapA = await cache.GetSnapshotAsync();
            Assert.Contains(customRoleIdA, (IEnumerable<Guid>)snapA.ActiveRoleIds);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-relief", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            Assert.Equal(1, await db.UserAccounts.CountAsync());
            Assert.Equal("Beta Relief Fund", (await db.OrganizationProfiles.SingleAsync()).Name);
            var singleRole = await db.SecurityRoles.SingleAsync();
            Assert.Equal(roleIdB, singleRole.Id);

            var snapB = await cache.GetSnapshotAsync();
            Assert.DoesNotContain(customRoleIdA, (IEnumerable<Guid>)snapB.ActiveRoleIds);
            Assert.Contains(roleIdB.Value, (IEnumerable<Guid>)snapB.ActiveRoleIds);
        }
    }

    private static async Task AssertAllIdentityTablesHaveForceRlsAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = 'identity' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Assert.True(reader.GetBoolean(1), $"Missing RLS on identity.{reader.GetString(0)}");
            Assert.True(
                reader.GetBoolean(2),
                $"Missing FORCE RLS on identity.{reader.GetString(0)}"
            );
        }

        Assert.True(count >= 10);
    }
}
