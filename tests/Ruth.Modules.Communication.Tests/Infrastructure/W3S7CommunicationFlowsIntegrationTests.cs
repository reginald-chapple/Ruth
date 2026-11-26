using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Tests.Fixtures;
using Ruth.Providers.CommunicationTransports;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

[Collection(CommunicationPostgresFixtures.Name)]
public sealed class W3S7CommunicationFlowsIntegrationTests(
    CommunicationPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task DripFlowSequencing_TimedDelays_SmartSkip_Conversions_Analytics_AndEvents_WorkAgainstPostgres18()
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

        var flowId = await CreateThreeStepRenewalFlowAsync(provider);
        var (r1, r2, _) = await EnrollThreeRecipientsAsync(provider, flowId);
        await RunImmediateStepAndConversionsAsync(provider, flowId, r1, r2);
        await AdvanceFutureStepsAndVerifyFunnelAnalyticsAsync(provider, flowId);
        await W3S7FlowAttendanceEventsTestHelper.VerifyAttendanceAutoConversionAndMergeAsync(
            provider
        );
    }

    private static async Task<Guid> CreateThreeStepRenewalFlowAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var seeded = (
            await queries.QueryAsync<
                GetCommunicationFlowsQuery,
                IReadOnlyList<CommunicationFlowResponse>
            >(new GetCommunicationFlowsQuery(ActiveOnly: true))
        ).Value;
        Assert.Contains(seeded, static f =>
            string.Equals(f.Key, "new-member-onboarding-drip", StringComparison.Ordinal)
        );

        var created = (
            await commands.SendAsync<CreateCommunicationFlowCommand, CommunicationFlowResponse>(
                new CreateCommunicationFlowCommand(
                    "Cohort Renewal Drip",
                    "cohort-renewal-drip",
                    CommunicationFlowCategory.MembershipAndRenewal,
                    [
                        new CommunicationFlowStepDto(
                            0,
                            "day-0-open",
                            "Renewal Window Open",
                            CommunicationChannel.Email,
                            DelayMinutes: 0,
                            SubjectTemplate: "Renew with {{ OrganizationName }}",
                            BodyTemplate: "<p>Hello {{ RecipientName }}, your renewal window is open.</p>"
                        ),
                        new CommunicationFlowStepDto(
                            1,
                            "day-3-followup",
                            "Day 3 Follow-Up",
                            CommunicationChannel.Email,
                            DelayMinutes: 4_320,
                            SubjectTemplate: "Day 3 Renewal Check-In",
                            BodyTemplate: "<p>Quick check-in on your membership.</p>"
                        ),
                        new CommunicationFlowStepDto(
                            2,
                            "day-7-unopened",
                            "Day 7 Unopened Nudge",
                            CommunicationChannel.Email,
                            DelayMinutes: 5_760,
                            SubjectTemplate: "Did you see our renewal note?",
                            BodyTemplate: "<p>Final friendly reminder.</p>",
                            SendOnlyIfPreviousUnopened: true
                        ),
                    ],
                    TopicKey: "billing-and-dues",
                    TriggerMode: FlowTriggerMode.OnDemandCohort,
                    ExitCondition: FlowExitCondition.ExitOnConversion,
                    ConversionGoalKind: FlowConversionGoalKind.ManualOrExternalGoal
                )
            )
        ).Value;
        return created.Id;
    }

    private static async Task<(Guid R1, Guid R2, Guid R3)> EnrollThreeRecipientsAsync(
        ServiceProvider provider,
        Guid flowId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var instance = (
            await commands.SendAsync<
                EnrollCommunicationFlowRecipientsCommand,
                CommunicationFlowInstanceResponse
            >(
                new EnrollCommunicationFlowRecipientsCommand(
                    flowId,
                    [
                        new FlowRecipientEnrollmentInputDto(
                            EmailAddress: "r1@example.org",
                            DisplayName: "Recipient One"
                        ),
                        new FlowRecipientEnrollmentInputDto(
                            EmailAddress: "r2@example.org",
                            DisplayName: "Recipient Two"
                        ),
                        new FlowRecipientEnrollmentInputDto(
                            EmailAddress: "r3@example.org",
                            DisplayName: "Recipient Three"
                        ),
                    ],
                    CohortLabel: "Fall Renewal Cohort"
                )
            )
        ).Value;

        Assert.Equal(3, instance.Recipients.Count);
        return (instance.Recipients[0].Id, instance.Recipients[1].Id, instance.Recipients[2].Id);
    }

    private static async Task RunImmediateStepAndConversionsAsync(
        ServiceProvider provider,
        Guid flowId,
        Guid r1Id,
        Guid r2Id
    )
    {
        var job = provider
            .GetServices<IScheduledJob>()
            .Single(static j =>
                string.Equals(
                    j.JobKey,
                    "ruth.communication.flow-advancement",
                    StringComparison.Ordinal
                )
            );
        var step0Run = await job.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, step0Run.Status);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        // R1 converts ($150) -> exits early
        _ = await commands.SendAsync<
            RecordFlowConversionCommand,
            CommunicationFlowConversionResponse
        >(
            new RecordFlowConversionCommand(
                r1Id,
                FlowConversionGoalKind.ManualOrExternalGoal,
                ConversionValue: 150m,
                ReferenceKey: "INV-2026-01",
                RecordStepOpen: true,
                RecordStepClick: true
            )
        );

        // R2 opens email without converting -> stays Active, will skip Step 2 (SendOnlyIfPreviousUnopened)
        _ = await commands.SendAsync<
            RecordFlowConversionCommand,
            CommunicationFlowConversionResponse
        >(new RecordFlowConversionCommand(r2Id, FlowConversionGoalKind.None, RecordStepOpen: true));

        // Re-running before +3 days sends 0 messages
        var premature = (
            await commands.SendAsync<
                ProcessDueCommunicationFlowsCommand,
                FlowAdvancementBatchResultResponse
            >(new ProcessDueCommunicationFlowsCommand(SpecificFlowId: flowId))
        ).Value;
        Assert.Equal(0, premature.StepsDispatched);
    }

    private static async Task AdvanceFutureStepsAndVerifyFunnelAnalyticsAsync(
        ServiceProvider provider,
        Guid flowId
    )
    {
        var baseFuture = DateTimeOffset.UtcNow.AddDays(4);
        await using (var scope1 = provider.CreateAsyncScope())
        {
            var cmds = scope1.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var step1Res = (
                await cmds.SendAsync<
                    ProcessDueCommunicationFlowsCommand,
                    FlowAdvancementBatchResultResponse
                >(
                    new ProcessDueCommunicationFlowsCommand(
                        SpecificFlowId: flowId,
                        EvaluationTimeOverrideUtc: baseFuture
                    )
                )
            ).Value;
            Assert.Equal(2, step1Res.StepsDispatched);
        }

        await using (var scope2 = provider.CreateAsyncScope())
        {
            var cmds = scope2.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope2.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var step2Res = (
                await cmds.SendAsync<
                    ProcessDueCommunicationFlowsCommand,
                    FlowAdvancementBatchResultResponse
                >(
                    new ProcessDueCommunicationFlowsCommand(
                        SpecificFlowId: flowId,
                        EvaluationTimeOverrideUtc: baseFuture.AddDays(5)
                    )
                )
            ).Value;
            Assert.Equal(1, step2Res.StepsDispatched);
            Assert.Equal(1, step2Res.StepsSkippedSmartRule);
            Assert.Equal(2, step2Res.RecipientsCompletedFlow);

            var analytics = (
                await queries.QueryAsync<
                    GetCommunicationFlowAnalyticsQuery,
                    CommunicationFlowAnalyticsResponse
                >(new GetCommunicationFlowAnalyticsQuery(flowId))
            ).Value;
            Assert.Equal(3, analytics.TotalEnrolled);
            Assert.Equal(0, analytics.ActiveInFlight);
            Assert.Equal(2, analytics.CompletedAllSteps);
            Assert.Equal(1, analytics.ConvertedExitedCount);
            Assert.Equal(150m, analytics.TotalConversionValue);
            Assert.Equal(33.33m, analytics.ConversionRatePercent);

            var instances = (
                await queries.QueryAsync<
                    GetCommunicationFlowInstancesQuery,
                    IReadOnlyList<CommunicationFlowInstanceResponse>
                >(new GetCommunicationFlowInstancesQuery(flowId))
            ).Value;
            Assert.Equal(FlowInstanceStatus.Completed, Assert.Single(instances).Status);
        }
    }
}
