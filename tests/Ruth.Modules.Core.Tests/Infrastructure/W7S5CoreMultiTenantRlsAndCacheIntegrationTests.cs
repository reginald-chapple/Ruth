using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class W7S5CoreMultiTenantRlsAndCacheIntegrationTests(
    CorePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_DeterministicEntityTypes_DefinedValuesSql_AndCoreReadCache_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(dbConn);
        await AssertAllCoreTablesHaveForceRlsAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var typeA = await SeedTenantAndCreateDefinedTypeAsync(
            provider,
            tenantA,
            "alpha-core",
            "Alpha Grant Tier"
        );
        var typeB = await SeedTenantAndCreateDefinedTypeAsync(
            provider,
            tenantB,
            "beta-core",
            "Beta Grant Tier"
        );

        await VerifyEntityTypesAndDefinedValuesIsolationAsync(
            provider,
            tenantA,
            typeA,
            tenantB,
            typeB
        );
        await VerifyCoreReadCacheTenantPartitioningAsync(provider, tenantA, tenantB);
    }

    private static async Task<Guid> SeedTenantAndCreateDefinedTypeAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string valueLabel
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<CoreTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantId);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var created = await commands.SendAsync<
                CreateDefinedTypeCommand,
                DefinedTypeDetailResponse
            >(new CreateDefinedTypeCommand("Assistance Tier", Description: "Shared name"));
            Assert.True(created.IsSuccess);

            var addedValue = await commands.SendAsync<AddDefinedValueCommand, DefinedValueResponse>(
                new AddDefinedValueCommand(
                    created.Value.Id,
                    valueLabel,
                    Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["segment"] = "priority",
                    }
                )
            );
            Assert.True(addedValue.IsSuccess);
            return created.Value.Id;
        }
    }

    private static async Task VerifyEntityTypesAndDefinedValuesIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid typeA,
        TenantId tenantB,
        Guid typeB
    )
    {
        IReadOnlyList<EntityTypeResponse> typesA;
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-core", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            typesA = (
                await queries.QueryAsync<GetEntityTypesQuery, IReadOnlyList<EntityTypeResponse>>(
                    new GetEntityTypesQuery()
                )
            ).Value;

            var matchedA = (
                await queries.QueryAsync<
                    QueryDefinedValuesByAttributeQuery,
                    IReadOnlyList<DefinedValueResponse>
                >(new QueryDefinedValuesByAttributeQuery(typeA, "segment", "priority"))
            ).Value;
            var singleA = Assert.Single(matchedA);
            Assert.Equal("Alpha Grant Tier", singleA.Value);

            var crossLookup = await queries.QueryAsync<
                QueryDefinedValuesByAttributeQuery,
                IReadOnlyList<DefinedValueResponse>
            >(new QueryDefinedValuesByAttributeQuery(typeB, "segment", "priority"));
            Assert.Empty(crossLookup.Value);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-core", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var typesB = (
                await queries.QueryAsync<GetEntityTypesQuery, IReadOnlyList<EntityTypeResponse>>(
                    new GetEntityTypesQuery()
                )
            ).Value;

            Assert.Equal(typesA.Count, typesB.Count);
            Assert.Empty(
                typesA.Select(static x => x.Id).Intersect(typesB.Select(static x => x.Id))
            );
        }
    }

    private static async Task VerifyCoreReadCacheTenantPartitioningAsync(
        ServiceProvider provider,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var cache = provider.GetRequiredService<ICoreReadCache>();
        var loadsA = 0;
        var loadsB = 0;

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-core", TenantLifecycleStage.Live))
        {
            var valA = await cache.GetOrLoadAsync(CoreReadCache.CategoriesBucket, _ =>
            {
                loadsA++;
                return Task.FromResult("alpha-payload");
            });
            Assert.Equal("alpha-payload", valA);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-core", TenantLifecycleStage.Live))
        {
            var valB = await cache.GetOrLoadAsync(CoreReadCache.CategoriesBucket, _ =>
            {
                loadsB++;
                return Task.FromResult("beta-payload");
            });
            Assert.Equal("beta-payload", valB);
            await cache.InvalidateBucketAsync(CoreReadCache.CategoriesBucket);
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-core", TenantLifecycleStage.Live))
        {
            var cachedA = await cache.GetOrLoadAsync(CoreReadCache.CategoriesBucket, _ =>
            {
                loadsA++;
                return Task.FromResult("reloaded-alpha");
            });
            Assert.Equal("alpha-payload", cachedA);
            Assert.Equal(1, loadsA);
            Assert.Equal(1, loadsB);
        }
    }

    private static async Task AssertAllCoreTablesHaveForceRlsAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = 'core' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Assert.True(reader.GetBoolean(1), $"Missing RLS on core.{reader.GetString(0)}");
            Assert.True(reader.GetBoolean(2), $"Missing FORCE RLS on core.{reader.GetString(0)}");
        }

        Assert.True(count >= 15);
    }
}
