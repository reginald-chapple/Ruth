using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Domain;

public sealed class W5S1PipelineDomainTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 25, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = new(2026, 11, 25);

    [Fact]
    public void PipelineType_Validation_EnforcesSlugStagesAndSequentialProgression()
    {
        var invalid = PipelineType.Create(new PipelineTypeSpec("", "BAD SLUG!", []));
        Assert.True(invalid.IsFailure);

        var spec = new PipelineTypeSpec(
            "Sequential Volunteer Intake",
            "seq-volunteer-intake",
            [
                new PipelineStageSpec("Step 1", 0, IsInitial: true),
                new PipelineStageSpec(
                    "Step 2",
                    1,
                    Automations:
                    [
                        new StageAutomationDto(
                            StageAutomationKind.SendSystemNotification,
                            SystemCommunicationKey: "engagement.stage-2",
                            AutoSetFollowUpDays: 5
                        ),
                    ]
                ),
                new PipelineStageSpec("Connected", 2, IsTerminalCompleted: true),
                new PipelineStageSpec("Closed", 3, IsTerminalInactive: true),
            ],
            RequiresPlacementGroup: true,
            IsSequentialStageProgression: true,
            ActivityTypes: [new PipelineActivityTypeSpec("Phone Call", 0)],
            Attributes: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Department"] = "Community",
            }
        );

        var pipelineRes = PipelineType.Create(spec);
        Assert.True(pipelineRes.IsSuccess);
        var pipeline = pipelineRes.Value;
        pipeline.SetAttributeValue("Region", "West");
        pipeline.RemoveAttributeValue("Department");
        Assert.Equal("West", pipeline.AttributeValues["Region"]);

        var branchA = Guid.CreateVersion7();
        var branchCoordinator = Guid.CreateVersion7();
        var fallbackCoordinator = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();

        var oppRes = EngagementOpportunity.Create(
            new EngagementOpportunitySpec(
                pipeline.Id.Value,
                "Mentor Cohort",
                "mentor-cohort",
                DefaultBranchId: branchA,
                DefaultConnectorPersonId: fallbackCoordinator,
                BranchConfigs: [new OpportunityBranchCoordinatorDto(branchA, branchCoordinator)],
                PlacementGroupConfigs: [new OpportunityPlacementGroupDto(groupId)]
            )
        );
        Assert.True(oppRes.IsSuccess);
        var opp = oppRes.Value;
        Assert.Equal(branchCoordinator, opp.ResolveDefaultConnector(branchA));
        Assert.Equal(fallbackCoordinator, opp.ResolveDefaultConnector(Guid.CreateVersion7()));

        var requester = Guid.CreateVersion7();
        var reqRes = EngagementRequest.Create(
            pipeline,
            opp,
            new EngagementRequestCreationSpec(requester, Comments: "Ready to mentor"),
            _today,
            _now
        );
        Assert.True(reqRes.IsSuccess);
        var req = reqRes.Value;
        Assert.Equal(branchCoordinator, req.AssignedConnectorPersonId);

        // Skipping from Step 1 directly to Connected fails sequential check
        var skip = req.TransitionToStage(
            pipeline,
            pipeline.Stages[2].Id.Value,
            actorPersonId: null,
            note: null,
            _today,
            _now
        );
        Assert.True(skip.IsFailure);

        // Moving to Step 2 succeeds and triggers AutoSetFollowUpDays = 5
        var step2 = req.TransitionToStage(
            pipeline,
            pipeline.Stages[1].Id.Value,
            branchCoordinator,
            "Moved",
            _today,
            _now
        );
        Assert.True(step2.IsSuccess);
        Assert.Equal(EngagementRequestState.FutureFollowUp, req.RequestState);
        Assert.Equal(_today.AddDays(5), req.FollowUpDate);

        // Wake due follow-up once 5 days elapse
        Assert.False(req.WakeIfDue(_today.AddDays(2), _now.AddDays(2)));
        Assert.True(req.WakeIfDue(_today.AddDays(5), _now.AddDays(5)));
        Assert.Equal(EngagementRequestState.Active, req.RequestState);

        // Terminal Completed requires PlacementGroupId when RequiresPlacementGroup is true
        var blockedConnect = req.TransitionToStage(
            pipeline,
            pipeline.Stages[2].Id.Value,
            actorPersonId: null,
            note: null,
            _today,
            _now
        );
        Assert.True(blockedConnect.IsFailure);

        var connected = req.MarkConnected(
            pipeline,
            groupId,
            placementGroupRoleId: null,
            branchCoordinator,
            "Placed",
            _now
        );
        Assert.True(connected.IsSuccess);
        Assert.Equal(EngagementRequestState.Connected, req.RequestState);
        Assert.NotEmpty(req.DomainEvents);

        var (_, completedHealth) = PipelineBoardEvaluator.EvaluateHealth(
            req,
            pipeline,
            _today,
            _now
        );
        Assert.Equal(EngagementRequestHealth.Completed, completedHealth);
    }

    [Fact]
    public void EngagementRequest_Health_Repoint_Transfer_AndInactive_WorkDeterministically()
    {
        var pipe = PipelineType
            .Create(
                new PipelineTypeSpec(
                    "General Intake",
                    "general-intake",
                    [
                        new PipelineStageSpec(
                            "Start",
                            0,
                            IsInitial: true,
                            IdleThresholdDaysOverride: 5
                        ),
                        new PipelineStageSpec("Inactive", 1, IsTerminalInactive: true),
                    ],
                    DaysUntilRequestIdle: 10
                )
            )
            .Value;

        var oldPerson = Guid.CreateVersion7();
        var newPerson = Guid.CreateVersion7();
        var opp = EngagementOpportunity
            .Create(
                new EngagementOpportunitySpec(
                    pipe.Id.Value,
                    "Welcome Desk",
                    "welcome-desk",
                    DefaultConnectorPersonId: oldPerson,
                    BranchConfigs:
                    [
                        new OpportunityBranchCoordinatorDto(Guid.CreateVersion7(), oldPerson),
                        new OpportunityBranchCoordinatorDto(
                            Guid.CreateVersion7(),
                            Guid.CreateVersion7()
                        ),
                    ],
                    Attributes: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Team"] = "Welcome",
                    }
                )
            )
            .Value;

        var req = EngagementRequest
            .Create(pipe, opp, new EngagementRequestCreationSpec(oldPerson), _today, _now)
            .Value;

        var (idle0, h0) = PipelineBoardEvaluator.EvaluateHealth(req, pipe, _today, _now.AddDays(1));
        Assert.Equal(1, idle0);
        Assert.Equal(EngagementRequestHealth.OnTrack, h0);

        var (_, hDueSoon) = PipelineBoardEvaluator.EvaluateHealth(
            req,
            pipe,
            _today,
            _now.AddDays(3)
        );
        Assert.Equal(EngagementRequestHealth.DueSoon, hDueSoon);

        var (_, hIdle) = PipelineBoardEvaluator.EvaluateHealth(req, pipe, _today, _now.AddDays(6));
        Assert.Equal(EngagementRequestHealth.Idle, hIdle);

        Assert.True(
            req.ScheduleFollowUp(
                pipe,
                _today.AddDays(4),
                oldPerson,
                "Call next week",
                _today,
                _now
            ).IsSuccess
        );
        var (_, hOverdue) = PipelineBoardEvaluator.EvaluateHealth(
            req,
            pipe,
            _today.AddDays(5),
            _now.AddDays(5)
        );
        Assert.Equal(EngagementRequestHealth.OverdueFollowUp, hOverdue);

        Assert.True(opp.RepointPerson(oldPerson, newPerson));
        Assert.True(req.RepointPerson(oldPerson, newPerson));
        Assert.Equal(newPerson, req.RequesterPersonId);
        Assert.Equal(newPerson, req.AssignedConnectorPersonId);

        var opp2 = EngagementOpportunity
            .Create(new EngagementOpportunitySpec(pipe.Id.Value, "Mentorship", "mentorship"))
            .Value;
        req.TransferTo(pipe, opp2, "Better fit", newPerson, _now);
        Assert.Equal(opp2.Id.Value, req.EngagementOpportunityId);

        req.MarkInactive(pipe, "Opted out", newPerson, _now);
        Assert.Equal(EngagementRequestState.Inactive, req.RequestState);
        Assert.Equal(
            EngagementRequestHealth.Inactive,
            PipelineBoardEvaluator.EvaluateHealth(req, pipe, _today, _now).Health
        );

        // Transitioning back to non-terminal stage re-activates request
        Assert.True(
            req.TransitionToStage(
                pipe,
                pipe.Stages[0].Id.Value,
                newPerson,
                "Reopened",
                _today,
                _now
            ).IsSuccess
        );
        Assert.Equal(EngagementRequestState.Active, req.RequestState);

        // Transitioning directly to terminal inactive stage sets Inactive
        Assert.True(
            req.TransitionToStage(
                pipe,
                pipe.Stages[1].Id.Value,
                newPerson,
                "Closed via stage",
                _today,
                _now
            ).IsSuccess
        );
        Assert.Equal(EngagementRequestState.Inactive, req.RequestState);
    }
}
