using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Slices.Groups;
using Ruth.Modules.Groups.Slices.GroupTypes;
using Ruth.Modules.Groups.Slices.Members;
using Ruth.Modules.Groups.Slices.Requirements;
using Ruth.Modules.Groups.Slices.Sync;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Groups;

internal static class GroupsCoreSliceRegistration
{
    public static void RegisterGroupTypeHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse,
            CreateGroupTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateGroupTypeCommand,
            GroupTypeDetailResponse,
            UpdateGroupTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddGroupTypeRoleCommand,
            GroupTypeDetailResponse,
            AddGroupTypeRoleCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateGroupTypeRoleCommand,
            GroupTypeDetailResponse,
            UpdateGroupTypeRoleCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveGroupTypeRoleCommand,
            GroupTypeDetailResponse,
            RemoveGroupTypeRoleCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupTypesQuery,
            IReadOnlyList<GroupTypeSummaryResponse>,
            GetGroupTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupTypeByIdQuery,
            GroupTypeDetailResponse,
            GetGroupTypeByIdQueryHandler
        >();
    }

    public static void RegisterGroupHandlers(IServiceCollection services)
    {
        RegisterGroupCommandHandlers(services);
        RegisterGroupQueryHandlers(services);
    }

    private static void RegisterGroupCommandHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateGroupCommand,
            GroupDetailResponse,
            CreateGroupCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateGroupCommand,
            GroupDetailResponse,
            UpdateGroupCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetGroupArchiveStateCommand,
            GroupDetailResponse,
            SetGroupArchiveStateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateGroupAttributesCommand,
            GroupDetailResponse,
            UpdateGroupAttributesCommandHandler
        >();
        _ = services.AddCommandHandler<
            AttachGroupLocationCommand,
            GroupDetailResponse,
            AttachGroupLocationCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveGroupLocationCommand,
            GroupDetailResponse,
            RemoveGroupLocationCommandHandler
        >();
        _ = services.AddCommandHandler<
            CreateGroupDemographicTypeCommand,
            GroupDemographicTypeResponse,
            CreateGroupDemographicTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetGroupDemographicValueCommand,
            GroupDemographicValueResponse,
            SetGroupDemographicValueCommandHandler
        >();
    }

    private static void RegisterGroupQueryHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetGroupByIdQuery,
            GroupDetailResponse,
            GetGroupByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupTreeQuery,
            IReadOnlyList<GroupTreeNodeResponse>,
            GetGroupTreeQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupDemographicTypesQuery,
            IReadOnlyList<GroupDemographicTypeResponse>,
            GetGroupDemographicTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupHistoryQuery,
            IReadOnlyList<GroupHistoricalResponse>,
            GetGroupHistoryQueryHandler
        >();
        _ = services.AddQueryHandler<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>,
            FindGroupsQueryHandler
        >();
    }

    public static void RegisterMemberAndSyncHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<GroupRequirementEvaluatorService>();
        _ = services.AddScoped<GroupMemberAddService>();
        _ = services.AddScoped<GroupSyncReconciliationService>();

        RegisterMembers(services);
        RegisterRequirements(services);
        RegisterSync(services);
    }

    private static void RegisterMembers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            AddGroupMemberCommand,
            GroupMemberResponse,
            AddGroupMemberCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateGroupMemberCommand,
            GroupMemberResponse,
            UpdateGroupMemberCommandHandler
        >();
        _ = services.AddCommandHandler<
            RemoveGroupMemberCommand,
            bool,
            RemoveGroupMemberCommandHandler
        >();
        _ = services.AddCommandHandler<
            PlaceGroupMembersCommand,
            GroupPlacementResultResponse,
            PlaceGroupMembersCommandHandler
        >();
        _ = services.AddCommandHandler<
            SimpleRegisterForGroupCommand,
            GroupMemberResponse,
            SimpleRegisterForGroupCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>,
            GetGroupMembersQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonGroupMembershipsQuery,
            IReadOnlyList<GroupMemberResponse>,
            GetPersonGroupMembershipsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupMemberHistoryQuery,
            IReadOnlyList<GroupMemberHistoricalResponse>,
            GetGroupMemberHistoryQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupPlacementBoardQuery,
            GroupPlacementBoardResponse,
            GetGroupPlacementBoardQueryHandler
        >();
    }

    private static void RegisterRequirements(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse,
            CreateGroupRequirementTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddGroupRequirementCommand,
            GroupRequirementResponse,
            AddGroupRequirementCommandHandler
        >();
        _ = services.AddCommandHandler<
            EvaluateGroupMemberRequirementsCommand,
            IReadOnlyList<GroupMemberRequirementResponse>,
            EvaluateGroupMemberRequirementsCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetGroupMemberRequirementManualStateCommand,
            GroupMemberRequirementResponse,
            SetGroupMemberRequirementManualStateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupRequirementTypesQuery,
            IReadOnlyList<GroupRequirementTypeResponse>,
            GetGroupRequirementTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupRequirementsQuery,
            IReadOnlyList<GroupRequirementResponse>,
            GetGroupRequirementsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupMemberRequirementsQuery,
            IReadOnlyList<GroupMemberRequirementResponse>,
            GetGroupMemberRequirementsQueryHandler
        >();
    }

    private static void RegisterSync(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            UpsertGroupSyncRuleCommand,
            GroupSyncRuleResponse,
            UpsertGroupSyncRuleCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteGroupSyncRuleCommand,
            bool,
            DeleteGroupSyncRuleCommandHandler
        >();
        _ = services.AddCommandHandler<
            RunGroupSyncCommand,
            IReadOnlyList<GroupSyncExecutionResponse>,
            RunGroupSyncCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGroupSyncRulesQuery,
            IReadOnlyList<GroupSyncRuleResponse>,
            GetGroupSyncRulesQueryHandler
        >();
    }
}
