using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Tests.Fixtures;
using Ruth.Providers.CommunicationTransports;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

[Collection(CommunicationPostgresFixtures.Name)]
public sealed class W3S6BulkCampaignsWebhooksAndSmsIntegrationTests(
    CommunicationPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task BulkCampaignLifecycle_BatchJob_Webhooks_Preferences_TwoWaySms_AndHtmlHint_WorkAgainstPostgres18()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CommunicationTestHostFactory.BuildAndMigrateAsync(
            cs,
            services =>
            {
                var cfg = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                _ = services.AddCommunicationTransportProviders(cfg);
            }
        );

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();

        await W3S6WebhooksAndTemplatesTestHelper.VerifyTemplatesAndHtmlExportAsync(
            commands,
            queries
        );
        await PreUnsubscribeRecipientAsync(commands, queries);
        var campaignId = await CreateAndApproveCampaignAsync(commands, retiredPersonId);
        var deliveredRecipientId = await RunDispatchAndVerifyOutboxAsync(
            scope.ServiceProvider,
            queries,
            campaignId
        );
        await using var postJobScope = provider.CreateAsyncScope();
        var postCommands = postJobScope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var postQueries = postJobScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await W3S6WebhooksAndTemplatesTestHelper.VerifyWebhooksAsync(
            postCommands,
            deliveredRecipientId
        );
        await W3S6TwoWaySmsTestHelper.VerifyTwoWaySmsAndPersonMergeAsync(
            postJobScope.ServiceProvider,
            postCommands,
            postQueries,
            retiredPersonId,
            survivingPersonId
        );
    }

    private static async Task PreUnsubscribeRecipientAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var prefResp = (
            await queries.QueryAsync<GetRecipientPreferencesQuery, CommunicationPreferenceResponse>(
                new GetRecipientPreferencesQuery(
                    DestinationAddress: "optedout@example.org",
                    Channel: CommunicationChannel.Email
                )
            )
        ).Value;

        _ = await commands.SendAsync<OneClickUnsubscribeCommand, CommunicationPreferenceResponse>(
            new OneClickUnsubscribeCommand(prefResp.PreferenceToken, "community-digest")
        );
    }

    private static async Task<Guid> CreateAndApproveCampaignAsync(
        ICommandDispatcher commands,
        Guid authorPersonId
    )
    {
        var created = (
            await commands.SendAsync<CreateCommunicationCommand, CommunicationDetailResponse>(
                new CreateCommunicationCommand(
                    "Autumn Digest",
                    CommunicationChannel.Email,
                    TopicKey: "community-digest",
                    IsBulk: true,
                    Subject: "Autumn News for {{ RecipientName | default:'Friend' }}",
                    Sections:
                    [
                        new EmailSectionDto(
                            0,
                            EmailSectionKind.RichText,
                            Heading: "Autumn Programs",
                            BodyMarkup: "<p>Join our fall cohorts.</p>"
                        ),
                    ],
                    Recipients:
                    [
                        new CommunicationRecipientInputDto(
                            PersonId: authorPersonId,
                            DestinationAddress: "active@example.org",
                            DisplayName: "Jordan Active"
                        ),
                        new CommunicationRecipientInputDto(
                            DestinationAddress: "optedout@example.org",
                            DisplayName: "Sam OptedOut"
                        ),
                    ],
                    CreatedByPersonId: authorPersonId
                )
            )
        ).Value;

        _ = await commands.SendAsync<
            SubmitCommunicationForApprovalCommand,
            CommunicationDetailResponse
        >(new SubmitCommunicationForApprovalCommand(created.Id));
        _ = await commands.SendAsync<ApproveCommunicationCommand, CommunicationDetailResponse>(
            new ApproveCommunicationCommand(created.Id, ReviewerPersonId: authorPersonId)
        );
        return created.Id;
    }

    private static async Task<Guid> RunDispatchAndVerifyOutboxAsync(
        IServiceProvider sp,
        IQueryDispatcher queries,
        Guid campaignId
    )
    {
        var dispatchJob = sp.GetServices<IScheduledJob>()
            .Single(static j =>
                string.Equals(
                    j.JobKey,
                    "ruth.communication.bulk-dispatch",
                    StringComparison.Ordinal
                )
            );
        var jobOutcome = await dispatchJob.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobOutcome.Status);

        var sentDetail = (
            await queries.QueryAsync<GetCommunicationByIdQuery, CommunicationDetailResponse>(
                new GetCommunicationByIdQuery(campaignId)
            )
        ).Value;
        Assert.Equal(CommunicationStatus.Sent, sentDetail.Status);
        var delivered = Assert.Single(sentDetail.Recipients, static r =>
            r.Status == RecipientDeliveryStatus.Delivered
        );
        _ = Assert.Single(sentDetail.Recipients, static r =>
            r.Status == RecipientDeliveryStatus.SkippedUnsubscribed
        );

        var db = sp.GetRequiredService<CommunicationDbContext>();
        Assert.Contains(db.OutboxMessages, static m =>
            m.EventType.Contains("BulkCommunicationSentIntegrationEvent", StringComparison.Ordinal)
        );
        return delivered.Id;
    }
}
