using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W5S1PipelineIntegrationTests(EngagementPostgresContainerFixture postgres)
{
    [Fact]
    public async Task Pipelines_PublicSignup_GroupPlacement_AndConnectorBoard_PersistEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var groupId = await CreatePlacementGroupAsync(commands);
        var (pipeline, opp) = await CreatePipelineAndOpportunityAsync(commands, groupId);

        var signup = await commands.SendAsync<
            SubmitPublicEngagementSignupCommand,
            EngagementRequestResponse
        >(
            new SubmitPublicEngagementSignupCommand(
                opp.Slug,
                "Elena",
                "Vance",
                "elena.vance@example.org",
                MobilePhone: "+13035550199",
                Comments: "Excited to volunteer"
            )
        );
        Assert.True(signup.IsSuccess);

        var act = await commands.SendAsync<
            RecordEngagementActivityCommand,
            EngagementRequestResponse
        >(
            new RecordEngagementActivityCommand(
                signup.Value.Id,
                ActivityTypeId: null,
                "Phone Call",
                Note: "Spoke with Elena"
            )
        );
        Assert.True(act.IsSuccess);

        var connRes = await commands.SendAsync<
            ConnectEngagementRequestCommand,
            EngagementRequestResponse
        >(
            new ConnectEngagementRequestCommand(
                signup.Value.Id,
                PlacementGroupId: groupId,
                Note: "Placed"
            )
        );
        Assert.True(connRes.IsSuccess);
        Assert.Equal(EngagementRequestState.Connected, connRes.Value.RequestState);

        var members = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(groupId));
        Assert.Contains(members.Value, m => m.PersonId == signup.Value.RequesterPersonId);

        var board = await queries.QueryAsync<GetConnectorBoardQuery, ConnectorBoardResponse>(
            new GetConnectorBoardQuery(pipeline.Id, IncludeClosed: true)
        );
        Assert.Equal(1, board.Value.ConnectedCount);
    }

    private static async Task<Guid> CreatePlacementGroupAsync(ICommandDispatcher commands)
    {
        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand(
                "Serving Teams",
                InitialRoles: [new GroupTypeRoleInputDto("Volunteer", IsLeader: false)]
            )
        );
        Assert.True(gt.IsSuccess);

        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Hospitality Welcome Team")
        );
        Assert.True(grp.IsSuccess);
        return grp.Value.Id;
    }

    private static async Task<(
        PipelineTypeResponse Pipeline,
        EngagementOpportunityResponse Opportunity
    )> CreatePipelineAndOpportunityAsync(ICommandDispatcher commands, Guid placementGroupId)
    {
        var pipe = await commands.SendAsync<CreatePipelineTypeCommand, PipelineTypeResponse>(
            new CreatePipelineTypeCommand(
                new PipelineTypeSpec(
                    "Volunteer Intake",
                    "volunteer-intake",
                    [
                        new PipelineStageSpec("New Signup", 0, IsInitial: true),
                        new PipelineStageSpec("Interview", 1),
                        new PipelineStageSpec("Connected", 2, IsTerminalCompleted: true),
                    ],
                    RequiresPlacementGroup: true,
                    ActivityTypes: [new PipelineActivityTypeSpec("Phone Call", 0)]
                )
            )
        );
        Assert.True(pipe.IsSuccess);

        var opp = await commands.SendAsync<
            CreateEngagementOpportunityCommand,
            EngagementOpportunityResponse
        >(
            new CreateEngagementOpportunityCommand(
                new EngagementOpportunitySpec(
                    pipe.Value.Id,
                    "Hospitality Intake",
                    "hospitality-intake",
                    PublicSummary: "Join the hospitality welcome team.",
                    PlacementGroupConfigs: [new OpportunityPlacementGroupDto(placementGroupId)]
                )
            )
        );
        Assert.True(opp.IsSuccess);
        return (pipe.Value, opp.Value);
    }
}
