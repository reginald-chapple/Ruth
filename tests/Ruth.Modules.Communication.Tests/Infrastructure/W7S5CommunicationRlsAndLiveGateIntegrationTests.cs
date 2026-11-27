using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.Modules.Communication.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

[Collection(CommunicationPostgresFixtures.Name)]
public sealed class W7S5CommunicationRlsAndLiveGateIntegrationTests(
    CommunicationPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task CommsForceRls_SandboxVsLiveGating_SinkPartitioning_AndPreferenceTokens_IsolateTenants()
    {
        var dbConn = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CommunicationTestHostFactory.BuildAndMigrateAsync(
            dbConn,
            services =>
            {
                _ = services.Configure<CommunicationTransportOptions>(opts =>
                    opts.PreferredEmailTransport = "fake-live-email"
                );
                _ = services.AddSingleton<ICommunicationTransport>(
                    new W7S5CommunicationRlsTestHelper.FakeExternalEmailTransport()
                );
            }
        );

        await W7S5CommunicationRlsTestHelper.AssertAllCommsTablesHaveForceRlsAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var campaignIdA = await VerifySandboxBulkAndFlowGatesAsync(provider, tenantA);
        await W7S5CommunicationRlsTestHelper.VerifySandboxSmsAndScheduledJobsAsync(
            provider,
            tenantA
        );
        var tokenA = await PromoteTenantAToLiveAndDispatchAsync(provider, tenantA, campaignIdA);
        await VerifyTenantBIsolationInSinkDbAndTokensAsync(provider, tenantB, tokenA);
    }

    private static async Task<Guid> VerifySandboxBulkAndFlowGatesAsync(
        ServiceProvider provider,
        TenantId tenantA
    )
    {
        using (
            AmbientTenantContext.BeginScope(
                tenantA,
                "sandbox-charity",
                TenantLifecycleStage.Sandbox
            )
        )
        {
            var resolver = provider.GetRequiredService<CommunicationTransportResolver>();
            Assert.Equal(
                DevInMemoryEmailTransport.Key,
                resolver.ResolveForChannel(CommunicationChannel.Email).TransportKey
            );

            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var created = (
                await commands.SendAsync<CreateCommunicationCommand, CommunicationDetailResponse>(
                    new CreateCommunicationCommand(
                        "Sandbox Appeal",
                        CommunicationChannel.Email,
                        TopicKey: "general-updates",
                        IsBulk: true,
                        Subject: "Hello Volunteer",
                        HtmlBodyOverride: "<p>Sandbox body</p>",
                        Recipients:
                        [
                            new CommunicationRecipientInputDto(
                                DestinationAddress: "ally@alpha.org",
                                DisplayName: "Ally"
                            ),
                        ]
                    )
                )
            ).Value;

            _ = await commands.SendAsync<
                SubmitCommunicationForApprovalCommand,
                CommunicationDetailResponse
            >(new SubmitCommunicationForApprovalCommand(created.Id));

            var blockedApprove = await commands.SendAsync<
                ApproveCommunicationCommand,
                CommunicationDetailResponse
            >(new ApproveCommunicationCommand(created.Id));
            Assert.Equal(
                BulkCommunicationErrors.TenantNotLive.Code,
                blockedApprove.PrimaryError.Code
            );

            var blockedBatch = await commands.SendAsync<
                ProcessDueBulkCommunicationsCommand,
                BulkDispatchBatchResultResponse
            >(new ProcessDueBulkCommunicationsCommand());
            Assert.Equal(
                BulkCommunicationErrors.TenantNotLive.Code,
                blockedBatch.PrimaryError.Code
            );

            var blockedFlows = await commands.SendAsync<
                ProcessDueCommunicationFlowsCommand,
                FlowAdvancementBatchResultResponse
            >(new ProcessDueCommunicationFlowsCommand());
            Assert.Equal(
                BulkCommunicationErrors.TenantNotLive.Code,
                blockedFlows.PrimaryError.Code
            );

            return created.Id;
        }
    }

    private static async Task<string> PromoteTenantAToLiveAndDispatchAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid campaignIdA
    )
    {
        using (
            AmbientTenantContext.BeginScope(tenantA, "sandbox-charity", TenantLifecycleStage.Live)
        )
        {
            var resolver = provider.GetRequiredService<CommunicationTransportResolver>();
            Assert.Equal(
                "fake-live-email",
                resolver.ResolveForChannel(CommunicationChannel.Email).TransportKey
            );

            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var approved = await commands.SendAsync<
                ApproveCommunicationCommand,
                CommunicationDetailResponse
            >(new ApproveCommunicationCommand(campaignIdA));
            Assert.True(approved.IsSuccess);

            var sink = provider.GetRequiredService<InMemoryCommunicationSink>();
            sink.Record(
                new OutboundTransportMessage(
                    Guid.CreateVersion7(),
                    CommunicationChannel.Email,
                    "ally@alpha.org",
                    "Ally",
                    Subject: "Tenant A Captured"
                ),
                DevInMemoryEmailTransport.Key,
                "dev-a-1",
                DateTimeOffset.UtcNow
            );
            _ = Assert.Single(sink.Query());

            var pref = (
                await queries.QueryAsync<
                    GetRecipientPreferencesQuery,
                    CommunicationPreferenceResponse
                >(
                    new GetRecipientPreferencesQuery(
                        DestinationAddress: "ally@alpha.org",
                        Channel: CommunicationChannel.Email
                    )
                )
            ).Value;
            return pref.PreferenceToken;
        }
    }

    private static async Task VerifyTenantBIsolationInSinkDbAndTokensAsync(
        ServiceProvider provider,
        TenantId tenantB,
        string tokenFromTenantA
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-charity", TenantLifecycleStage.Live))
        {
            var sink = provider.GetRequiredService<InMemoryCommunicationSink>();
            Assert.Empty(sink.Query());

            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var commsB = (
                await queries.QueryAsync<
                    GetCommunicationsQuery,
                    IReadOnlyList<CommunicationSummaryResponse>
                >(new GetCommunicationsQuery())
            ).Value;
            Assert.Empty(commsB);

            var replayUnsub = await commands.SendAsync<
                OneClickUnsubscribeCommand,
                CommunicationPreferenceResponse
            >(new OneClickUnsubscribeCommand(tokenFromTenantA, "general-updates"));
            Assert.True(replayUnsub.IsFailure);
            Assert.Equal(
                BulkCommunicationErrors.PreferenceTokenInvalid.Code,
                replayUnsub.PrimaryError.Code
            );
        }
    }
}
