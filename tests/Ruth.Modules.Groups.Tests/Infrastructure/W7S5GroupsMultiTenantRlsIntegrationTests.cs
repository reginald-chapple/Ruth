using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W7S5GroupsMultiTenantRlsIntegrationTests(
    GroupsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_NullsNotDistinctIndexes_AndGroupHierarchy_IsolateTenantsUnderRuntimeRole()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(dbConn);
        await AssertForceRlsAndNullsNotDistinctIndexesAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var groupA = await SeedGroupAndNullableRequirementAsync(
            provider,
            tenantA,
            "alpha-groups",
            expectDuplicateRejected: true
        );
        var groupB = await SeedGroupAndNullableRequirementAsync(
            provider,
            tenantB,
            "beta-groups",
            expectDuplicateRejected: false
        );

        await VerifyCrossTenantGroupIsolationAsync(provider, tenantA, groupA, tenantB, groupB);
    }

    private static async Task<Guid> SeedGroupAndNullableRequirementAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        bool expectDuplicateRejected
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
                new CreateGroupTypeCommand(
                    Name: "Community Aid Teams",
                    InitialRoles: [new GroupTypeRoleInputDto("Volunteer")]
                )
            );
            Assert.True(gt.IsSuccess);

            var group = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
                new CreateGroupCommand(gt.Value.Id, "Food Pantry Shift", IsPublic: true)
            );
            Assert.True(group.IsSuccess);

            var reqType = await commands.SendAsync<
                CreateGroupRequirementTypeCommand,
                GroupRequirementTypeResponse
            >(
                new CreateGroupRequirementTypeCommand(
                    Name: "Safety Orientation",
                    CheckType: RequirementCheckType.Manual
                )
            );
            Assert.True(reqType.IsSuccess);

            var req = await commands.SendAsync<
                AddGroupRequirementCommand,
                GroupRequirementResponse
            >(
                new AddGroupRequirementCommand(
                    reqType.Value.Id,
                    GroupId: group.Value.Id,
                    GroupTypeId: null,
                    GroupRoleId: null
                )
            );
            Assert.True(req.IsSuccess);

            if (expectDuplicateRejected)
            {
                var duplicate = await commands.SendAsync<
                    AddGroupRequirementCommand,
                    GroupRequirementResponse
                >(
                    new AddGroupRequirementCommand(
                        reqType.Value.Id,
                        GroupId: group.Value.Id,
                        GroupTypeId: null,
                        GroupRoleId: null
                    )
                );
                Assert.True(duplicate.IsFailure);
            }

            return group.Value.Id;
        }
    }

    private static async Task VerifyCrossTenantGroupIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid groupA,
        TenantId tenantB,
        Guid groupB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-groups", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GroupsDbContext>();
            var visibleIds = await db.Groups.Select(static g => g.Id.Value).ToListAsync();
            var onlyB = Assert.Single(visibleIds);
            Assert.Equal(groupB, onlyB);
            Assert.DoesNotContain(groupA, visibleIds);
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-groups", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GroupsDbContext>();
            var visibleIds = await db.Groups.Select(static g => g.Id.Value).ToListAsync();
            var onlyA = Assert.Single(visibleIds);
            Assert.Equal(groupA, onlyA);
        }
    }

    private static async Task AssertForceRlsAndNullsNotDistinctIndexesAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
                + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
                + "WHERE n.nspname = 'groups' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on groups.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on groups.{reader.GetString(0)}"
                );
            }

            Assert.True(count >= 15);
        }

        await using var idxCmd = conn.CreateCommand();
        idxCmd.CommandText =
            "SELECT indexname FROM pg_indexes "
            + "WHERE schemaname = 'groups' AND indexdef ILIKE '%NULLS NOT DISTINCT%'";
        var nullsNotDistinctIndexes = new List<string>();
        await using var idxReader = await idxCmd.ExecuteReaderAsync();
        while (await idxReader.ReadAsync())
        {
            nullsNotDistinctIndexes.Add(idxReader.GetString(0));
        }

        Assert.Equal(3, nullsNotDistinctIndexes.Count);
    }
}
