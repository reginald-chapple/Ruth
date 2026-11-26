using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Slices.Following;
using Ruth.Modules.Engagement.Slices.Reminders;
using Ruth.Modules.Engagement.Slices.SupportRequests;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Engagement.Slices;

internal static class W5S3SupportReminderRegistration
{
    public static void Register(IServiceCollection services)
    {
        _ = services.AddScheduledJob<ExpiredSupportRequestCleanupJob>();
        _ = services.AddScheduledJob<ReminderAndFollowingDigestJob>();

        RegisterSupportHandlers(services);
        RegisterReminderHandlers(services);
        RegisterFollowingHandlers(services);
    }

    private static void RegisterSupportHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateSupportCategoryCommand,
            SupportCategoryResponse,
            CreateSupportCategoryCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSupportCategoriesQuery,
            IReadOnlyList<SupportCategoryResponse>,
            GetSupportCategoriesQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitSupportRequestCommand,
            SupportRequestResponse,
            SubmitSupportRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            ModerateSupportRequestCommand,
            SupportRequestResponse,
            ModerateSupportRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddSupportRequestCommentCommand,
            SupportRequestResponse,
            AddSupportRequestCommentCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordCommunitySupportCommand,
            SupportRequestResponse,
            RecordCommunitySupportCommandHandler
        >();
        _ = services.AddCommandHandler<
            FlagSupportRequestCommand,
            SupportRequestResponse,
            FlagSupportRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            ResolveSupportRequestCommand,
            SupportRequestResponse,
            ResolveSupportRequestCommandHandler
        >();
        _ = services.AddCommandHandler<
            ExpireSupportRequestsCommand,
            SupportExpirationCleanupResponse,
            ExpireSupportRequestsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCareTeamReviewQueueQuery,
            IReadOnlyList<SupportRequestResponse>,
            GetCareTeamReviewQueueQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPublicSupportWallQuery,
            IReadOnlyList<SupportRequestResponse>,
            GetPublicSupportWallQueryHandler
        >();
    }

    private static void RegisterReminderHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateReminderTypeCommand,
            ReminderTypeResponse,
            CreateReminderTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetReminderTypesQuery,
            IReadOnlyList<ReminderTypeResponse>,
            GetReminderTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateReminderCommand,
            ReminderResponse,
            CreateReminderCommandHandler
        >();
        _ = services.AddCommandHandler<
            CompleteReminderCommand,
            ReminderResponse,
            CompleteReminderCommandHandler
        >();
        _ = services.AddCommandHandler<
            ProcessDueRemindersCommand,
            ReminderProcessRunResponse,
            ProcessDueRemindersCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRemindersQuery,
            IReadOnlyList<ReminderResponse>,
            GetRemindersQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonNotificationsQuery,
            IReadOnlyList<NotificationMessageResponse>,
            GetPersonNotificationsQueryHandler
        >();
        _ = services.AddCommandHandler<
            MarkNotificationReadCommand,
            NotificationMessageResponse,
            MarkNotificationReadCommandHandler
        >();
    }

    private static void RegisterFollowingHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            ToggleFollowingSubscriptionCommand,
            FollowingSubscriptionResponse,
            ToggleFollowingSubscriptionCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonFollowingsQuery,
            IReadOnlyList<FollowingSubscriptionResponse>,
            GetPersonFollowingsQueryHandler
        >();
        _ = services.AddCommandHandler<
            RecordFollowedEntityEventCommand,
            Guid,
            RecordFollowedEntityEventCommandHandler
        >();
        _ = services.AddCommandHandler<
            DispatchFollowingDigestsCommand,
            FollowingDigestRunResponse,
            DispatchFollowingDigestsCommandHandler
        >();
    }
}
