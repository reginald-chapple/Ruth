using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W5S3SupportRemindersIntegrationTests(
    EngagementPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task SupportRequests_PiiRedaction_Reminders_Notifications_AndFollowingDigests_WorkEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var coordinatorId = Guid.CreateVersion7();
        var subjectId = Guid.CreateVersion7();
        await VerifySupportModerationAndPublicWallAsync(
            commands,
            queries,
            coordinatorId,
            subjectId
        );
        await VerifyRemindersNotificationsAndDigestsAsync(
            scope.ServiceProvider,
            commands,
            queries,
            coordinatorId,
            subjectId
        );
    }

    private static async Task VerifySupportModerationAndPublicWallAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid coordinatorId,
        Guid subjectId
    )
    {
        var cat = await commands.SendAsync<CreateSupportCategoryCommand, SupportCategoryResponse>(
            new CreateSupportCategoryCommand(
                new SupportCategorySpec(
                    "Community Care",
                    "community-care",
                    DefaultCoordinatorPersonId: coordinatorId,
                    AutoFlagHoldThreshold: 2
                )
            )
        );
        Assert.True(cat.IsSuccess);

        var sub = await commands.SendAsync<SubmitSupportRequestCommand, SupportRequestResponse>(
            new SubmitSupportRequestCommand(
                cat.Value.Id,
                "Maya",
                "Need meal train after surgery",
                LastName: "Chen",
                Email: "maya.chen@example.org",
                RequesterPersonId: subjectId,
                VisibilityLevel: SupportVisibilityLevel.PublicCommunityWall,
                IsUrgent: true
            )
        );
        Assert.True(sub.IsSuccess);
        var reqId = sub.Value.Id;

        Assert.Empty(
            (
                await queries.QueryAsync<
                    GetPublicSupportWallQuery,
                    IReadOnlyList<SupportRequestResponse>
                >(new GetPublicSupportWallQuery())
            ).Value
        );

        _ = await commands.SendAsync<ModerateSupportRequestCommand, SupportRequestResponse>(
            new ModerateSupportRequestCommand(reqId, SupportModerationDecision.Approve)
        );
        _ = await commands.SendAsync<AddSupportRequestCommentCommand, SupportRequestResponse>(
            new AddSupportRequestCommentCommand(
                reqId,
                "Care Coordinator",
                "Private caseworker note",
                IsInternalCareNote: true
            )
        );
        _ = await commands.SendAsync<AddSupportRequestCommentCommand, SupportRequestResponse>(
            new AddSupportRequestCommentCommand(
                reqId,
                "Neighbor Sam",
                "Bringing dinner Tuesday!",
                IsInternalCareNote: false
            )
        );
        _ = await commands.SendAsync<RecordCommunitySupportCommand, SupportRequestResponse>(
            new RecordCommunitySupportCommand(reqId, IsCareTeamMember: false)
        );

        var wall = await queries.QueryAsync<
            GetPublicSupportWallQuery,
            IReadOnlyList<SupportRequestResponse>
        >(new GetPublicSupportWallQuery());
        var publicItem = Assert.Single(wall.Value);
        Assert.Equal("Maya", publicItem.FirstName);
        Assert.Null(publicItem.LastName);
        Assert.Null(publicItem.Email);
        _ = Assert.Single(publicItem.Comments);

        _ = await commands.SendAsync<FlagSupportRequestCommand, SupportRequestResponse>(
            new FlagSupportRequestCommand(reqId)
        );
        var held = await commands.SendAsync<FlagSupportRequestCommand, SupportRequestResponse>(
            new FlagSupportRequestCommand(reqId)
        );
        Assert.Equal(SupportModerationState.Flagged, held.Value.ModerationState);
    }

    private static async Task VerifyRemindersNotificationsAndDigestsAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid coordinatorId,
        Guid subjectId
    )
    {
        var rt = await commands.SendAsync<CreateReminderTypeCommand, ReminderTypeResponse>(
            new CreateReminderTypeCommand(
                new ReminderTypeSpec(
                    "Follow-Up Call",
                    NotificationFlavor: ReminderNotificationFlavor.InAppAndSystemNotification,
                    SystemCommunicationKey: "engagement.reminder-due"
                )
            )
        );
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _ = await commands.SendAsync<CreateReminderCommand, ReminderResponse>(
            new CreateReminderCommand(
                rt.Value.Id,
                coordinatorId,
                "Ruth.Crm.Person",
                subjectId,
                "Maya Chen",
                "Check post-op recovery",
                today
            )
        );
        _ = await commands.SendAsync<
            ToggleFollowingSubscriptionCommand,
            FollowingSubscriptionResponse
        >(
            new ToggleFollowingSubscriptionCommand(
                coordinatorId,
                "Ruth.Crm.Person",
                subjectId,
                "Maya Chen"
            )
        );

        var digestJob = sp.GetServices<IScheduledJob>()
            .First(static j =>
                string.Equals(
                    j.JobKey,
                    "engagement.reminders-and-following-digest",
                    StringComparison.Ordinal
                )
            );
        var jobRes = await digestJob.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobRes.Status);

        var inbox = await queries.QueryAsync<
            GetPersonNotificationsQuery,
            IReadOnlyList<NotificationMessageResponse>
        >(new GetPersonNotificationsQuery(coordinatorId, UnreadOnly: true));
        Assert.True(inbox.Value.Count >= 3);

        var read = await commands.SendAsync<
            MarkNotificationReadCommand,
            NotificationMessageResponse
        >(new MarkNotificationReadCommand(inbox.Value[0].Id));
        Assert.True(read.Value.IsRead);
    }
}
