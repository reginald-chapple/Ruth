using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class W7S8SubsystemIsolationCanaryTests(HostContainersFixture containers)
{
    private const string TenantAdminPassword = "TaxExemptStrong#2026!";

    [Fact]
    public async Task EndToEnd_TenantA_Vs_TenantB_Vs_UnsetTenant_Isolates_Http_Outbox_Caches_Search_Storage_And_PgClassRls()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            }
        );

        await CrossTenantCanaryTestHelper.AssertPostgresForceRlsOnAllTenantTablesAsync(isolatedDb);

        var (tenantARaw, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Blue Ridge Relief Alliance",
            "Blue Ridge Relief",
            "blue-ridge",
            "52-4445556",
            "ops@blueridge.org",
            TenantAdminPassword
        );
        var (tenantBRaw, hostB) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Chesapeake Community Clinic",
            "Chesapeake Clinic",
            "chesapeake-clinic",
            "52-7778889",
            "ops@chesapeakeclinic.org",
            TenantAdminPassword
        );
        var tenantA = TenantId.From(tenantARaw);
        var tenantB = TenantId.From(tenantBRaw);

        var (tokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            TenantAdminPassword
        );
        var (tokenB, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostB,
            TenantAdminPassword
        );

        await PublishCmsItemsStagingOutboxEventsAsync(
            factory.Services,
            tenantA,
            "AlphaDisasterFellowshipToken",
            tenantB,
            "BetaPediatricClinicToken"
        );
        await W7S8SubsystemCanaryHelper.DrainCmsOutboxFromUnresolvedBackgroundScopeAsync(
            factory.Services
        );

        await VerifyHttpAndSpotlightSearchIsolationAsync(
            client,
            factory.Services,
            hostA,
            tokenA,
            hostB,
            tokenB
        );
        await W7S8SubsystemCanaryHelper.VerifyAllCachesPartitionByTenantAndFailClosedAsync(
            factory.Services,
            tenantA,
            tenantB
        );
        await W7S8SubsystemCanaryHelper.VerifyRustFsStorageEnforcesTenantPrefixAndBlocksCrossTenantAsync(
            factory.Services,
            tenantA,
            tenantB
        );

        // Final sweep AFTER all tenant tables contain rich data: ruth_app with unset GUC MUST see 0 rows on every table in all 15 schemas
        await CrossTenantCanaryTestHelper.AssertAppRoleZeroRowsAndPlatformDenyAsync(
            isolatedDb,
            factory.Services
        );
    }

    private static async Task PublishCmsItemsStagingOutboxEventsAsync(
        IServiceProvider services,
        TenantId tenantA,
        string uniqueTermA,
        TenantId tenantB,
        string uniqueTermB
    )
    {
        await PublishSingleTenantChannelItemAsync(services, tenantA, "blue-ridge", uniqueTermA);
        await PublishSingleTenantChannelItemAsync(
            services,
            tenantB,
            "chesapeake-clinic",
            uniqueTermB
        );
    }

    private static async Task PublishSingleTenantChannelItemAsync(
        IServiceProvider services,
        TenantId tenantId,
        string slug,
        string uniqueSearchTitle
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Sandbox))
        {
            await using var scope = services.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var chType = (
                await commands.SendAsync<
                    CreateContentChannelTypeCommand,
                    ContentChannelTypeResponse
                >(new CreateContentChannelTypeCommand("Bulletins", "bulletins"))
            ).Value;

            var channel = (
                await commands.SendAsync<CreateContentChannelCommand, ContentChannelResponse>(
                    new CreateContentChannelCommand(
                        chType.Id,
                        "Impact Bulletins",
                        "impact-bulletins",
                        RequiresApproval: false
                    )
                )
            ).Value;

            var item = await commands.SendAsync<
                CreateContentChannelItemCommand,
                ContentChannelItemResponse
            >(
                new CreateContentChannelItemCommand(
                    channel.Id,
                    uniqueSearchTitle,
                    "canary-item",
                    $"<p>{uniqueSearchTitle} body</p>",
                    Summary: uniqueSearchTitle
                )
            );
            Assert.True(item.IsSuccess);
        }
    }

    private static async Task VerifyHttpAndSpotlightSearchIsolationAsync(
        HttpClient client,
        IServiceProvider services,
        string hostA,
        string tokenA,
        string hostB,
        string tokenB
    )
    {
        // Tenant A Spotlight + Public Search find AlphaDisasterFellowshipToken and NEVER BetaPediatricClinicToken
        using var searchOwnA = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/search/spotlight?q=AlphaDisasterFellowshipToken",
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, searchOwnA.StatusCode);
        using var ownDocA = JsonDocument.Parse(await searchOwnA.Content.ReadAsStringAsync());
        Assert.Equal(1, ownDocA.RootElement.GetProperty("totalAuthorizedHits").GetInt32());

        using var searchCrossFromA = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/search/public?q=BetaPediatricClinicToken"
            )
        );
        Assert.Equal(HttpStatusCode.OK, searchCrossFromA.StatusCode);
        using var crossDocA = JsonDocument.Parse(
            await searchCrossFromA.Content.ReadAsStringAsync()
        );
        Assert.Equal(0, crossDocA.RootElement.GetProperty("totalAuthorizedHits").GetInt32());

        // Tenant B Spotlight + Public Search find BetaPediatricClinicToken and NEVER AlphaDisasterFellowshipToken
        using var searchOwnB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/search/spotlight?q=BetaPediatricClinicToken",
                bearerToken: tokenB
            )
        );
        Assert.Equal(HttpStatusCode.OK, searchOwnB.StatusCode);
        using var ownDocB = JsonDocument.Parse(await searchOwnB.Content.ReadAsStringAsync());
        Assert.Equal(1, ownDocB.RootElement.GetProperty("totalAuthorizedHits").GetInt32());

        using var searchCrossFromB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/search/spotlight?q=AlphaDisasterFellowshipToken",
                bearerToken: tokenB
            )
        );
        Assert.Equal(HttpStatusCode.OK, searchCrossFromB.StatusCode);
        using var crossDocB = JsonDocument.Parse(
            await searchCrossFromB.Content.ReadAsStringAsync()
        );
        Assert.Equal(0, crossDocB.RootElement.GetProperty("totalAuthorizedHits").GetInt32());

        await W7S8SubsystemCanaryHelper.VerifyUnresolvedSearchEngineReturnsEmptyAsync(
            services,
            "AlphaDisasterFellowshipToken"
        );
    }
}
