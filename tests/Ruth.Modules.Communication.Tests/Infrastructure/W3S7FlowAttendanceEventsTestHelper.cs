using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

internal static class W3S7FlowAttendanceEventsTestHelper
{
    public static async Task VerifyAttendanceAutoConversionAndMergeAsync(ServiceProvider provider)
    {
        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();
        Guid onboardingFlowId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var flows = (
                await queries.QueryAsync<
                    GetCommunicationFlowsQuery,
                    IReadOnlyList<CommunicationFlowResponse>
                >(new GetCommunicationFlowsQuery(ActiveOnly: true))
            ).Value;
            onboardingFlowId = flows
                .Single(static f =>
                    string.Equals(f.Key, "new-member-onboarding-drip", StringComparison.Ordinal)
                )
                .Id;

            _ = await commands.SendAsync<
                EnrollCommunicationFlowRecipientsCommand,
                CommunicationFlowInstanceResponse
            >(
                new EnrollCommunicationFlowRecipientsCommand(
                    onboardingFlowId,
                    [
                        new FlowRecipientEnrollmentInputDto(
                            PersonId: retiredPersonId,
                            EmailAddress: "newcomer@example.org",
                            DisplayName: "Casey Newcomer"
                        ),
                    ]
                )
            );

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var attendanceConsumers = scope.ServiceProvider.GetServices<
                IIntegrationEventConsumer<PersonAttendedIntegrationEvent>
            >();
            foreach (var consumer in attendanceConsumers)
            {
                await consumer.ConsumeAsync(
                    new PersonAttendedIntegrationEvent(
                        EventId: Guid.CreateVersion7(),
                        OccurredOnUtc: DateTimeOffset.UtcNow,
                        AttendanceId: Guid.CreateVersion7(),
                        AttendanceOccurrenceId: Guid.CreateVersion7(),
                        PersonId: retiredPersonId,
                        GroupId: Guid.CreateVersion7(),
                        GroupTypeId: Guid.CreateVersion7(),
                        BranchId: null,
                        LocationId: null,
                        ScheduleId: null,
                        OccurrenceDate: today,
                        WeekEndDate: today,
                        IsFirstTime: true,
                        CountsTowardHeadlineAttendance: true
                    )
                );
            }
        }

        await using (var mergeScope = provider.CreateAsyncScope())
        {
            await W3S5CrossModuleEventsTestHelper.ConsumePersonMergeAsync(
                mergeScope.ServiceProvider,
                retiredPersonId,
                survivingPersonId
            );

            var queries = mergeScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var analytics = (
                await queries.QueryAsync<
                    GetCommunicationFlowAnalyticsQuery,
                    CommunicationFlowAnalyticsResponse
                >(new GetCommunicationFlowAnalyticsQuery(onboardingFlowId))
            ).Value;
            Assert.Equal(1, analytics.TotalConversions);
            Assert.Equal(1, analytics.ConvertedExitedCount);
        }
    }
}
