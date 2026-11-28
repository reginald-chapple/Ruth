using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class W7S8SubsystemCanaryHelper
{
    public static async Task DrainCmsOutboxFromUnresolvedBackgroundScopeAsync(
        IServiceProvider services
    )
    {
        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            var cmsProcessor = services
                .GetServices<IOutboxModuleProcessor>()
                .Single(static p => string.Equals(p.Schema, "cms", StringComparison.Ordinal));
            var processed = await cmsProcessor.ProcessBatchAsync(CancellationToken.None);
            Assert.True(
                processed >= 2,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected fair-share Cms Outbox processor to dispatch >= 2 tenant events, saw {processed}."
                )
            );
        }
    }

    public static async Task VerifyUnresolvedSearchEngineReturnsEmptyAsync(
        IServiceProvider services,
        string queryTerm
    )
    {
        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            await using var scope = services.CreateAsyncScope();
            var engine = scope.ServiceProvider.GetRequiredService<PostgreSqlFullTextSearchEngine>();
            var res = await engine.SearchAsync(
                new ExecuteUniversalSearchQuery(queryTerm, PublicOnly: true),
                CancellationToken.None
            );
            Assert.Empty(res.Hits);
        }
    }

    public static async Task VerifyAllCachesPartitionByTenantAndFailClosedAsync(
        IServiceProvider services,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var coreCache = services.GetRequiredService<ICoreReadCache>();
        var aclCache = services.GetRequiredService<IAclSnapshotCache>();
        var personA = Guid.CreateVersion7();
        var personB = Guid.CreateVersion7();

        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            var valA = await coreCache.GetOrLoadAsync("canary-bucket", static _ =>
                Task.FromResult("ALPHA-CACHE-PAYLOAD")
            );
            Assert.Equal("ALPHA-CACHE-PAYLOAD", valA);

            var aclA = await aclCache.GetSnapshotAsync();
            Assert.NotEmpty(aclA.ActiveRoleIds);

            await using var scopeA = services.CreateAsyncScope();
            var segA = scopeA.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();
            await segA.SetSegmentMembersAsync("major-supporters", [personA]);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta", TenantLifecycleStage.Live))
        {
            var valB = await coreCache.GetOrLoadAsync("canary-bucket", static _ =>
                Task.FromResult("BETA-CACHE-PAYLOAD")
            );
            Assert.Equal("BETA-CACHE-PAYLOAD", valB);

            var aclB = await aclCache.GetSnapshotAsync();
            Assert.NotEmpty(aclB.ActiveRoleIds);
            Assert.Empty(
                aclB.ActiveRoleIds.Intersect(await LoadRoleIdsForTenantAsync(aclCache, tenantA))
            );

            await using var scopeB = services.CreateAsyncScope();
            var segB = scopeB.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();
            await segB.SetSegmentMembersAsync("major-supporters", [personB]);
            Assert.True(await segB.IsMemberAsync("major-supporters", personB));
            Assert.False(await segB.IsMemberAsync("major-supporters", personA));
        }

        // Invalidating Tenant A bucket must NOT evict Tenant B's cached value
        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            await coreCache.InvalidateBucketAsync("canary-bucket");
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta", TenantLifecycleStage.Live))
        {
            var stillCachedB = await coreCache.GetOrLoadAsync("canary-bucket", static _ =>
                Task.FromResult("UNEXPECTED-RELOAD")
            );
            Assert.Equal("BETA-CACHE-PAYLOAD", stillCachedB);
        }

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            var emptyAcl = await aclCache.GetSnapshotAsync();
            Assert.Empty(emptyAcl.ActiveRoleIds);

            var bypassedVal = await coreCache.GetOrLoadAsync("canary-bucket", static _ =>
                Task.FromResult("UNSET-BYPASSED-NO-CACHE")
            );
            Assert.Equal("UNSET-BYPASSED-NO-CACHE", bypassedVal);

            await using var unsetScope = services.CreateAsyncScope();
            var unsetSeg =
                unsetScope.ServiceProvider.GetRequiredService<IPersonalizationSegmentCache>();
            Assert.False(await unsetSeg.IsMemberAsync("major-supporters", personB));
        }
    }

    public static async Task VerifyRustFsStorageEnforcesTenantPrefixAndBlocksCrossTenantAsync(
        IServiceProvider services,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var storage = services.GetRequiredService<IStorageProvider>();
        Assert.Equal("rustfs", storage.ProviderKey);

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            await using var unsetStream = new MemoryStream("orphan"u8.ToArray());
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                storage.UploadAsync("audits/form-990.pdf", unsetStream, "application/pdf")
            );
        }

        string storedKeyA;
        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            await using var streamA = new MemoryStream(
                Encoding.UTF8.GetBytes("TENANT-A-IRS-990-CONFIDENTIAL")
            );
            var upA = await storage.UploadAsync("audits/form-990.pdf", streamA, "application/pdf");
            storedKeyA = upA.ObjectKey;
            Assert.Equal($"tenants/{tenantA}/audits/form-990.pdf", storedKeyA);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta", TenantLifecycleStage.Live))
        {
            // Tenant B has not uploaded audits/form-990.pdf yet -> ExistsAsync must be false!
            Assert.False(await storage.ExistsAsync("audits/form-990.pdf"));

            // Tenant B explicitly passing Tenant A's prefixed key MUST throw CrossTenantAccessException
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                storage.OpenReadAsync(storedKeyA)
            );
            _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                storage.DeleteAsync(storedKeyA)
            );

            // Tenant B uploads its own colliding relative key "audits/form-990.pdf"
            await using var streamB = new MemoryStream(
                Encoding.UTF8.GetBytes("TENANT-B-IRS-990-SEPARATE")
            );
            var upB = await storage.UploadAsync("audits/form-990.pdf", streamB, "application/pdf");
            Assert.Equal($"tenants/{tenantB}/audits/form-990.pdf", upB.ObjectKey);

            await using var readB = await storage.OpenReadAsync("audits/form-990.pdf");
            Assert.NotNull(readB);
            using var readerB = new StreamReader(readB, Encoding.UTF8);
            Assert.Equal("TENANT-B-IRS-990-SEPARATE", await readerB.ReadToEndAsync());
        }

        // Tenant A's object remains intact
        using (AmbientTenantContext.BeginScope(tenantA, "alpha", TenantLifecycleStage.Live))
        {
            await using var readA = await storage.OpenReadAsync("audits/form-990.pdf");
            Assert.NotNull(readA);
            using var readerA = new StreamReader(readA, Encoding.UTF8);
            Assert.Equal("TENANT-A-IRS-990-CONFIDENTIAL", await readerA.ReadToEndAsync());
        }
    }

    private static async Task<IReadOnlyCollection<Guid>> LoadRoleIdsForTenantAsync(
        IAclSnapshotCache aclCache,
        TenantId tenantId
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, "alpha", TenantLifecycleStage.Live))
        {
            var snap = await aclCache.GetSnapshotAsync();
            return snap.ActiveRoleIds;
        }
    }
}
