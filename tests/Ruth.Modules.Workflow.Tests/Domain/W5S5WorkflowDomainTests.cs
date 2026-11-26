using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Domain.Triggers;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Domain;

public sealed class W5S5WorkflowDomainTests
{
    [Fact]
    public void WorkflowType_CreateUpdateSequenceAndRepointPerson_EnforcesInvariants()
    {
        var retiredId = Guid.CreateVersion7();
        var survivingId = Guid.CreateVersion7();

        var invalidNoInitial = WorkflowType.Create(
            new WorkflowTypeSpec(
                "Broken",
                "broken-wf",
                "Item",
                "BRK-",
                [
                    new WorkflowActivityTypeSpec(
                        "step-1",
                        "Step 1",
                        Order: 0,
                        IsActivatedWithWorkflow: false
                    ),
                ]
            )
        );
        Assert.True(invalidNoInitial.IsFailure);

        var dupAttrs = WorkflowType.Create(
            new WorkflowTypeSpec(
                "Dup",
                "dup-wf",
                "Item",
                "DUP-",
                [
                    new WorkflowActivityTypeSpec(
                        "step-1",
                        "Step 1",
                        Order: 0,
                        IsActivatedWithWorkflow: true
                    ),
                ],
                Attributes:
                [
                    new WorkflowAttributeSpec("Status", "Status"),
                    new WorkflowAttributeSpec("status", "Duplicate"),
                ]
            )
        );
        Assert.True(dupAttrs.IsFailure);

        var created = WorkflowType.Create(BuildSampleSpec(retiredId));
        Assert.True(created.IsSuccess);
        var wfType = created.Value;

        Assert.Equal("INC-00001", wfType.AllocateNextWorkflowNumber());
        Assert.Equal("INC-00002", wfType.AllocateNextWorkflowNumber());
        Assert.NotNull(wfType.FindActivityByKey("TRIAGE"));
        Assert.Null(wfType.FindActivityByKey("missing"));

        wfType.SetAttributeValue("Department", "Operations");
        Assert.Equal("Operations", wfType.AttributeValues["department"]);
        wfType.RemoveAttributeValue("Department");
        Assert.False(wfType.AttributeValues.ContainsKey("department"));

        Assert.True(wfType.RepointPerson(retiredId, survivingId));
        Assert.False(wfType.RepointPerson(retiredId, survivingId));
        Assert.Equal(survivingId, wfType.Activities[0].DefaultAssignedPersonId);

        var updated = wfType.Update(
            BuildSampleSpec(survivingId) with
            {
                Name = "Updated Intake",
                Activities =
                [
                    .. BuildSampleSpec(survivingId).Activities,
                    new WorkflowActivityTypeSpec("escalation", "Escalation", Order: 2),
                ],
            }
        );
        Assert.True(updated.IsSuccess);
        Assert.Equal(3, wfType.Activities.Count);
    }

    [Fact]
    public void WorkflowInstance_LifecycleTransitionsLoggingRetriesAndCompletion_WorkDeterministically()
    {
        var initiator = Guid.CreateVersion7();
        var assignee = Guid.CreateVersion7();
        var surviving = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 25, 18, 0, 0, TimeSpan.Zero);

        var wfType = WorkflowType.Create(BuildSampleSpec(assignee)).Value;
        var launched = WorkflowInstance.Launch(
            wfType,
            customName: null,
            initiatorPersonId: initiator,
            primaryEntityTypeKey: "Ruth.Crm.Person",
            primaryEntityId: initiator,
            initialAttributes: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Priority"] = "High",
            },
            utcNow: now
        );
        Assert.True(launched.IsSuccess);
        var inst = launched.Value;
        Assert.Equal(WorkflowInstanceStatus.Active, inst.Status);
        Assert.Equal("High", inst.Attributes["priority"]);
        Assert.Equal("Standard", inst.Attributes["Tier"]);
        _ = Assert.Single(inst.Activities);

        var openAct = inst.Activities[0];
        var assignRes = inst.AssignActivity(
            openAct.Id,
            personId: assignee,
            groupId: null,
            loggingLevel: WorkflowLoggingLevel.Action,
            utcNow: now,
            actorPersonId: initiator
        );
        Assert.True(assignRes.IsSuccess);
        Assert.True(
            inst.AssignActivity(
                WorkflowActivityId.Create(),
                personId: assignee,
                groupId: null,
                loggingLevel: WorkflowLoggingLevel.Action,
                utcNow: now
            ).IsFailure
        );

        var resolutionDef = wfType.FindActivityByKey("resolution")!;
        var act2 = inst.ActivateActivity(resolutionDef, WorkflowLoggingLevel.Action, now);
        Assert.True(act2.IsSuccess);
        var idempAct2 = inst.ActivateActivity(resolutionDef, WorkflowLoggingLevel.Action, now);
        Assert.Equal(act2.Value.Id, idempAct2.Value.Id);

        inst.PauseForUserInput(now.AddMinutes(1));
        Assert.Equal(WorkflowInstanceStatus.WaitingForInput, inst.Status);
        Assert.Null(inst.NextScheduledProcessAtUtc);

        inst.SuspendUntil(now.AddMinutes(10), now.AddMinutes(2));
        Assert.Equal(WorkflowInstanceStatus.Suspended, inst.Status);

        inst.ResumeProcessing(now.AddMinutes(11));
        Assert.Equal(WorkflowInstanceStatus.Active, inst.Status);
        inst.ScheduleNextPass(120, now.AddMinutes(11));
        Assert.Equal(now.AddMinutes(13), inst.NextScheduledProcessAtUtc);

        inst.RecordExecutionFailure(
            "Transient timeout",
            maxAttempts: 2,
            retryDelaySeconds: 30,
            now
        );
        Assert.Equal(WorkflowInstanceStatus.Active, inst.Status);

        Assert.True(inst.RepointPerson(initiator, surviving));
        Assert.Equal(surviving, inst.InitiatorPersonId);

        inst.UpdateStateLabel("Resolved");
        var completed = inst.Complete("All steps done", WorkflowLoggingLevel.Workflow, now);
        Assert.True(completed.IsSuccess);
        Assert.True(inst.IsTerminal);
        Assert.Equal("Resolved", inst.StateLabel);
        _ = Assert.Single(inst.DomainEvents.OfType<WorkflowCompletedDomainEvent>());
        Assert.True(inst.Cancel("Too late", WorkflowLoggingLevel.Workflow, now).IsFailure);
    }

    [Fact]
    public void WorkflowInstance_FaultThresholdAndLoggingLevels_EnforcedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var inactiveSpec = BuildSampleSpec(defaultAssignee: null) with { IsActive = false };
        var inactiveType = WorkflowType.Create(inactiveSpec).Value;
        Assert.True(
            WorkflowInstance
                .Launch(
                    inactiveType,
                    customName: null,
                    initiatorPersonId: null,
                    primaryEntityTypeKey: null,
                    primaryEntityId: null,
                    initialAttributes: null,
                    utcNow: now
                )
                .IsFailure
        );

        var activeType = WorkflowType
            .Create(
                BuildSampleSpec(defaultAssignee: null) with
                {
                    LoggingLevel = WorkflowLoggingLevel.None,
                    MaxProcessingAttempts = 1,
                }
            )
            .Value;
        var inst = WorkflowInstance
            .Launch(
                activeType,
                customName: "Custom",
                initiatorPersonId: null,
                primaryEntityTypeKey: null,
                primaryEntityId: null,
                initialAttributes: null,
                utcNow: now
            )
            .Value;
        Assert.Empty(inst.Logs);

        inst.ResumeProcessing(now);
        inst.RecordExecutionFailure(
            "Fatal plugin error",
            maxAttempts: 1,
            retryDelaySeconds: 10,
            now
        );
        Assert.Equal(WorkflowInstanceStatus.Faulted, inst.Status);
        _ = Assert.Single(inst.Logs);
    }

    [Theory]
    [InlineData(WorkflowTriggerOperator.Always, null, null, true)]
    [InlineData(WorkflowTriggerOperator.Equals, "Urgent", "urgent", true)]
    [InlineData(WorkflowTriggerOperator.Equals, "Normal", "urgent", false)]
    [InlineData(WorkflowTriggerOperator.NotEquals, "Normal", "urgent", true)]
    [InlineData(WorkflowTriggerOperator.Contains, "Emergency Grant", "grant", true)]
    [InlineData(WorkflowTriggerOperator.IsNotEmpty, "value", null, true)]
    [InlineData(WorkflowTriggerOperator.IsNotEmpty, "   ", null, false)]
    [InlineData(WorkflowTriggerOperator.GreaterThan, "250.50", "100", true)]
    [InlineData(WorkflowTriggerOperator.GreaterThan, "50", "100", false)]
    public void WorkflowTriggerEvaluator_EvaluatesAllOperatorsCaseInsensitively(
        WorkflowTriggerOperator op,
        string? actualValue,
        string? expectedValue,
        bool expectedMatch
    )
    {
        var wfTypeId = Guid.CreateVersion7();
        var spec = new WorkflowTriggerSpec(
            "Trigger Rule",
            WorkflowTriggerKind.IntegrationEvent,
            "engagement.support.submitted",
            wfTypeId,
            EntityTypeQualifier: "Ruth.Engagement.SupportRequest",
            FilterAttributeKey: op == WorkflowTriggerOperator.Always ? null : "Category",
            FilterOperator: op,
            FilterExpectedValue: expectedValue
        );

        var triggerRes = WorkflowTrigger.Create(spec);
        Assert.True(triggerRes.IsSuccess);

        var attrs = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["category"] = actualValue,
        };
        var matched = WorkflowTriggerEvaluator.Matches(
            triggerRes.Value,
            WorkflowTriggerKind.IntegrationEvent,
            "ENGAGEMENT.SUPPORT.SUBMITTED",
            "Ruth.Engagement.SupportRequest",
            attrs
        );
        Assert.Equal(expectedMatch, matched);

        Assert.False(
            WorkflowTriggerEvaluator.Matches(
                triggerRes.Value,
                WorkflowTriggerKind.IntegrationEvent,
                "engagement.support.submitted",
                "Ruth.Other.Entity",
                attrs
            )
        );
    }

    private static WorkflowTypeSpec BuildSampleSpec(Guid? defaultAssignee)
    {
        return new WorkflowTypeSpec(
            "Incident Triage",
            "incident-triage",
            "Incident",
            "INC-",
            [
                new WorkflowActivityTypeSpec(
                    "triage",
                    "Triage",
                    Order: 0,
                    IsActivatedWithWorkflow: true,
                    DefaultAssignedPersonId: defaultAssignee,
                    Actions:
                    [
                        new WorkflowActionTypeSpec(
                            "set-tier",
                            "Set Tier",
                            "workflow.attributes.set-value",
                            Order: 0
                        ),
                    ]
                ),
                new WorkflowActivityTypeSpec(
                    "resolution",
                    "Resolution",
                    Order: 1,
                    IsActivatedWithWorkflow: false
                ),
            ],
            Attributes:
            [
                new WorkflowAttributeSpec("Priority", "Priority", "text", "Normal", Order: 0),
                new WorkflowAttributeSpec("Tier", "Tier", "text", "Standard", Order: 1),
            ]
        );
    }
}
