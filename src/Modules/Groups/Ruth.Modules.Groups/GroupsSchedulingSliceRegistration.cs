using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Slices.Attendances;
using Ruth.Modules.Groups.Slices.Scheduling;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Groups;

internal static class GroupsSchedulingSliceRegistration
{
    public static void RegisterSchedulingHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<ScheduleVolunteerService>();
        RegisterConfigAndExclusionHandlers(services);
        RegisterRosterAndRsvpHandlers(services);
    }

    public static void RegisterAttendanceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse,
            RecordGroupAttendanceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupAttendanceOccurrencesQuery,
            IReadOnlyList<AttendanceOccurrenceSummaryResponse>,
            GetGroupAttendanceOccurrencesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAttendanceOccurrenceByIdQuery,
            AttendanceOccurrenceDetailResponse,
            GetAttendanceOccurrenceByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAttendanceMetricsSummaryQuery,
            AttendanceMetricsSummaryResponse,
            GetAttendanceMetricsSummaryQueryHandler
        >();
    }

    private static void RegisterConfigAndExclusionHandlers(IServiceCollection services)
    {
        RegisterConfigCommandHandlers(services);
        RegisterConfigQueryHandlers(services);
    }

    private static void RegisterConfigCommandHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateScheduleTemplateCommand,
            GroupMemberScheduleTemplateResponse,
            CreateScheduleTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpsertLocationScheduleConfigCommand,
            GroupLocationScheduleConfigResponse,
            UpsertLocationScheduleConfigCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpsertGroupMemberAssignmentCommand,
            GroupMemberAssignmentResponse,
            UpsertGroupMemberAssignmentCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveGroupMemberAssignmentCommand,
            bool,
            RemoveGroupMemberAssignmentCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddPersonScheduleExclusionCommand,
            PersonScheduleExclusionResponse,
            AddPersonScheduleExclusionCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemovePersonScheduleExclusionCommand,
            bool,
            RemovePersonScheduleExclusionCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddGroupScheduleExclusionCommand,
            GroupScheduleExclusionResponse,
            AddGroupScheduleExclusionCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveGroupScheduleExclusionCommand,
            bool,
            RemoveGroupScheduleExclusionCommandHandler
        >();
    }

    private static void RegisterConfigQueryHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetScheduleTemplatesQuery,
            IReadOnlyList<GroupMemberScheduleTemplateResponse>,
            GetScheduleTemplatesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetLocationScheduleConfigsQuery,
            IReadOnlyList<GroupLocationScheduleConfigResponse>,
            GetLocationScheduleConfigsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupMemberAssignmentsQuery,
            IReadOnlyList<GroupMemberAssignmentResponse>,
            GetGroupMemberAssignmentsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupScheduleExclusionsQuery,
            IReadOnlyList<GroupScheduleExclusionResponse>,
            GetGroupScheduleExclusionsQueryHandler
        >();
    }

    private static void RegisterRosterAndRsvpHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse,
            ScheduleVolunteerCommandHandler
        >();
        _ = services.AddCommandHandler<
            UnscheduleVolunteerCommand,
            bool,
            UnscheduleVolunteerCommandHandler
        >();
        _ = services.AddCommandHandler<
            AutoPopulateServingScheduleCommand,
            AutoPopulateScheduleResponse,
            AutoPopulateServingScheduleCommandHandler
        >();
        _ = services.AddCommandHandler<
            RespondToRsvpCommand,
            ScheduledVolunteerResponse,
            RespondToRsvpCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetServingScheduleGridQuery,
            ServingScheduleGridResponse,
            GetServingScheduleGridQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetVolunteerScheduleToolboxQuery,
            VolunteerScheduleToolboxResponse,
            GetVolunteerScheduleToolboxQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRsvpByTokenQuery,
            ScheduledVolunteerResponse,
            GetRsvpByTokenQueryHandler
        >();
    }
}
