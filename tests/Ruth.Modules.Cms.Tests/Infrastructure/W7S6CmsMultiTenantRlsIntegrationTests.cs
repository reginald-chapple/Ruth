using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.Modules.Cms.Tests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Infrastructure;

[Collection(CmsFixtures.Name)]
public sealed class W7S6CmsMultiTenantRlsIntegrationTests(CmsContainersFixture containers)
{
    [Fact]
    public async Task ForceRls_BackgroundBufferedInteractions_RedisSegmentKeys_AndCollidingSiteSlugs_IsolateTenants()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var redis = await containers.ConnectRedisAsync();
        await using var provider = await CmsTestHostFactory.CreateAsync(dbConn, redis);

        await AssertAllCmsTablesHaveForceRlsAndFailClosedAsync(dbConn, provider);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var personA = Guid.CreateVersion7();
        var personB = Guid.CreateVersion7();

        await SeedSiteAndEnqueueBufferedInteractionsAsync(
            provider,
            tenantA,
            "alpha-cms",
            "Alpha Community Site",
            personA
        );
        await SeedSiteAndEnqueueBufferedInteractionsAsync(
            provider,
            tenantB,
            "beta-cms",
            "Beta Aid Site",
            personB
        );

        await FlushBufferFromUnresolvedBackgroundContextAsync(provider);
        await VerifyPersistedInteractionsSitesAndRedisSegmentsIsolateAsync(
            provider,
            tenantA,
            personA,
            tenantB,
            personB
        );
    }

    private static async Task SeedSiteAndEnqueueBufferedInteractionsAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string siteName,
        Guid personId
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            await CmsTenantSeedContributor.EnsureBuiltInBlockTypesAsync(db, CancellationToken.None);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var site = await commands.SendAsync<CreateCmsSiteCommand, CmsSiteResponse>(
                new CreateCmsSiteCommand(siteName, "community-portal")
            );
            Assert.True(site.IsSuccess);

            var ingest = await commands.SendAsync<
                IngestInteractionsCommand,
                InteractionBufferStatusResponse
            >(
                new IngestInteractionsCommand(
                    [
                        new(
                            "portal-web",
                            "donate-cta",
                            "Donate CTA",
                            "Click",
                            personId,
                            $"sess-{slug}",
                            $"Click on {slug}"
                        ),
                    ],
                    FlushImmediately: false
                )
            );
            Assert.True(ingest.IsSuccess);
            Assert.Equal(1, ingest.Value.PendingApproximateCount);

            var segCache = scope.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();
            await segCache.SetSegmentMembersAsync("active-supporters", [personId]);
        }
    }

    private static async Task FlushBufferFromUnresolvedBackgroundContextAsync(
        ServiceProvider provider
    )
    {
        var buffer = provider.GetRequiredService<IInteractionBuffer>();
        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(async () =>
                await buffer.EnqueueAsync(
                    new TrackInteractionItemSpec("portal-web", "orphan", "Orphan", "View")
                )
            );

            Assert.Equal(2, buffer.GetStatus().PendingApproximateCount);
            var flushed = await buffer.FlushAsync();
            Assert.Equal(2, flushed.EnqueuedCount);
            Assert.Equal(0, flushed.PendingApproximateCount);
        }
    }

    private static async Task VerifyPersistedInteractionsSitesAndRedisSegmentsIsolateAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid personA,
        TenantId tenantB,
        Guid personB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-cms", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            var segCache = scope.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();

            var sitesA = (
                await queries.QueryAsync<GetCmsSitesQuery, IReadOnlyList<CmsSiteResponse>>(
                    new GetCmsSitesQuery(IncludeInactive: true)
                )
            ).Value;
            var singleSiteA = Assert.Single(sitesA);
            Assert.Equal("Alpha Community Site", singleSiteA.Name);

            var interactionsA = await db.Interactions.AsNoTracking().ToListAsync();
            var singleIntA = Assert.Single(interactionsA);
            Assert.Equal(personA, singleIntA.PersonId);

            Assert.True(await segCache.IsMemberAsync("active-supporters", personA));
            Assert.False(await segCache.IsMemberAsync("active-supporters", personB));
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-cms", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            var segCache = scope.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();

            var interactionsB = await db.Interactions.AsNoTracking().ToListAsync();
            var singleIntB = Assert.Single(interactionsB);
            Assert.Equal(personB, singleIntB.PersonId);

            Assert.True(await segCache.IsMemberAsync("active-supporters", personB));
            Assert.False(await segCache.IsMemberAsync("active-supporters", personA));
        }
    }

    private static async Task AssertAllCmsTablesHaveForceRlsAndFailClosedAsync(
        string adminConn,
        ServiceProvider provider
    )
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
                + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
                + "WHERE n.nspname = 'cms' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on cms.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on cms.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 30,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 30 tables in cms schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM cms.sites";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
