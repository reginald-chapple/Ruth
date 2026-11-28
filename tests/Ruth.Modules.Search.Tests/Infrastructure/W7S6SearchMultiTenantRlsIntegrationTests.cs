using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.Modules.Search.Tests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Search.Tests.Infrastructure;

[Collection(SearchFixtures.Name)]
public sealed class W7S6SearchMultiTenantRlsIntegrationTests(SearchContainersFixture containers)
{
    [Fact]
    public async Task ForceRls_WeightedTsvector_TrigramSearch_AndCollectionKeys_IsolateTenantsAndFailClosed()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var docA = Guid.CreateVersion7();
        var docB = Guid.CreateVersion7();

        var tenantProvider = new TenantScopedFakeSearchProvider(
            new Dictionary<TenantId, IReadOnlyList<SearchDocumentUpsertSpec>>
            {
                [tenantA] =
                [
                    new SearchDocumentUpsertSpec(
                        "tenant.docs",
                        "Crm",
                        "Ruth.Crm.Person",
                        docA,
                        "Humanitarian Relief Coordinator",
                        Subtitle: "ALPHA-ONLY",
                        Summary: "Confidential humanitarian grant roster for Alpha Relief.",
                        IsPublic: true
                    ),
                ],
                [tenantB] =
                [
                    new SearchDocumentUpsertSpec(
                        "tenant.docs",
                        "Crm",
                        "Ruth.Crm.Person",
                        docB,
                        "Humanitarian Food Pantry Lead",
                        Subtitle: "BETA-ONLY",
                        Summary: "Confidential humanitarian distribution schedule for Beta Pantry.",
                        IsPublic: true
                    ),
                ],
            }
        );

        var (provider, _) = await SearchTestHostFactory.CreateAsync(dbConn, [tenantProvider]);
        await using var _ = provider;

        await AssertAllSearchTablesHaveForceRlsAndFailClosedAsync(dbConn, provider);

        await IndexTenantCollectionAsync(provider, tenantA, "alpha-search");
        await IndexTenantCollectionAsync(provider, tenantB, "beta-search");

        await VerifyFullTextTrigramAndUnresolvedFailClosedAsync(
            provider,
            tenantA,
            docA,
            tenantB,
            docB
        );
    }

    private static async Task IndexTenantCollectionAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var coll = await commands.SendAsync<
                CreateContentCollectionCommand,
                ContentCollectionResponse
            >(
                new CreateContentCollectionCommand(
                    "Shared Portal Index",
                    "portal-index",
                    [new ContentCollectionSourceSpec("tenant.docs", 50, 1.0)]
                )
            );
            Assert.True(coll.IsSuccess);

            var rebuild = await commands.SendAsync<
                RebuildSearchIndexCommand,
                SearchIndexRebuildResponse
            >(new RebuildSearchIndexCommand(coll.Value.Id, "tenant.docs"));
            Assert.True(rebuild.IsSuccess);
            Assert.Equal(1, rebuild.Value.DocumentsUpserted);
        }
    }

    private static async Task VerifyFullTextTrigramAndUnresolvedFailClosedAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid docA,
        TenantId tenantB,
        Guid docB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-search", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var resA = (
                await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
                    new ExecuteUniversalSearchQuery(
                        "humanitarian",
                        CollectionKey: "portal-index",
                        PublicOnly: true
                    )
                )
            ).Value;
            var hitA = Assert.Single(resA.Hits);
            Assert.Equal(docA, hitA.EntityId);
            Assert.Equal("ALPHA-ONLY", hitA.Subtitle);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-search", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var resB = (
                await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
                    new ExecuteUniversalSearchQuery(
                        "humanitarin", // deliberate typo exercising pg_trgm similarity
                        CollectionKey: "portal-index",
                        PublicOnly: true
                    )
                )
            ).Value;
            var hitB = Assert.Single(resB.Hits);
            Assert.Equal(docB, hitB.EntityId);
            Assert.Equal("BETA-ONLY", hitB.Subtitle);
        }

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            await using var scope = provider.CreateAsyncScope();
            var engine = scope.ServiceProvider.GetRequiredService<PostgreSqlFullTextSearchEngine>();
            var emptyRes = await engine.SearchAsync(
                new ExecuteUniversalSearchQuery("humanitarian", PublicOnly: true),
                CancellationToken.None
            );
            Assert.Empty(emptyRes.Hits);
        }
    }

    private static async Task AssertAllSearchTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'search' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on search.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on search.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 5,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 5 tables in search schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM search.search_documents";
        Assert.Equal(
            0L,
            Convert.ToInt64(await appCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
        );
    }

    private sealed class TenantScopedFakeSearchProvider(
        IReadOnlyDictionary<TenantId, IReadOnlyList<SearchDocumentUpsertSpec>> docsByTenant
    ) : ISearchDocumentProvider
    {
        public string ProviderKey => "tenant.docs";
        public string ModuleName => "Crm";
        public string EntityTypeKey => "Ruth.Crm.Person";

        public Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
            SearchProviderLoadRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var tid = AmbientTenantContext.CurrentTenantId;
            return Task.FromResult<IReadOnlyList<SearchDocumentUpsertSpec>>(
                !tid.IsEmpty && docsByTenant.TryGetValue(tid, out var docs)
                    ?
                    [
                        .. docs.Select(d =>
                            d with
                            {
                                ContentCollectionId = request.ContentCollectionId,
                            }
                        ),
                    ]
                    : []
            );
        }
    }
}
