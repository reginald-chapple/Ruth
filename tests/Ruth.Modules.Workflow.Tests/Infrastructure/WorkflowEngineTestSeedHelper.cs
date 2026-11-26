using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

internal static class WorkflowEngineTestSeedHelper
{
    public static Dictionary<string, string?> Map(params (string Key, string? Value)[] entries)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in entries)
        {
            dict[k] = v;
        }

        return dict;
    }

    public static async Task<(
        Guid GroupId,
        Guid AccountId,
        Guid OpportunityId
    )> SeedCrossModuleTargetsAsync(ICommandDispatcher commands)
    {
        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand(
                "Sponsor Circles",
                InitialRoles: [new GroupTypeRoleInputDto("Member", IsLeader: false)]
            )
        );
        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Major Sponsors Cohort")
        );

        var acct = await commands.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new CreateFinancialAccountCommand(
                new FinancialAccountSpec(
                    "Annual Membership Dues",
                    "Membership Dues",
                    "MEM-DUES-WF",
                    FinancialAccountKind.OperatingRevenue,
                    IsTaxDeductible: false
                )
            )
        );

        var pipe = await commands.SendAsync<CreatePipelineTypeCommand, PipelineTypeResponse>(
            new CreatePipelineTypeCommand(
                new PipelineTypeSpec(
                    "Sponsor Stewarding",
                    "sponsor-stewarding",
                    [new PipelineStageSpec("New", 0, IsInitial: true)]
                )
            )
        );
        var opp = await commands.SendAsync<
            CreateEngagementOpportunityCommand,
            EngagementOpportunityResponse
        >(
            new CreateEngagementOpportunityCommand(
                new EngagementOpportunitySpec(
                    pipe.Value.Id,
                    "Sponsor Welcome Call",
                    "sponsor-welcome-call"
                )
            )
        );

        return (grp.Value.Id, acct.Value.Id, opp.Value.Id);
    }

    public static async Task<WorkflowTypeResponse> CreateFullIntegrationWorkflowTypeAsync(
        ICommandDispatcher commands,
        Guid groupId,
        Guid accountId,
        Guid opportunityId
    )
    {
        var spec = new WorkflowTypeSpec(
            "Sponsor Onboarding Pipeline",
            "sponsor-onboarding-pipeline",
            "Onboarding",
            "SPN-",
            [BuildProvisionActivity(groupId, accountId, opportunityId), BuildCooldownActivity()],
            Attributes: [new WorkflowAttributeSpec("StepCounter", "Step Counter", "number", "0")]
        );

        var created = await commands.SendAsync<CreateWorkflowTypeCommand, WorkflowTypeResponse>(
            new CreateWorkflowTypeCommand(spec)
        );
        Assert.True(created.IsSuccess);
        return created.Value;
    }

    private static WorkflowActivityTypeSpec BuildProvisionActivity(
        Guid groupId,
        Guid accountId,
        Guid opportunityId
    )
    {
        return new WorkflowActivityTypeSpec(
            "provision",
            "Provision Records",
            Order: 0,
            IsActivatedWithWorkflow: true,
            Actions:
            [
                new WorkflowActionTypeSpec(
                    "inc-counter",
                    "Increment Step Counter",
                    "workflow.attributes.set-value",
                    Order: 0,
                    Configuration: Map(("AttributeKey", "StepCounter"), ("IncrementBy", "1"))
                ),
                new WorkflowActionTypeSpec(
                    "ensure-person",
                    "Ensure CRM Person",
                    "workflow.crm.ensure-person",
                    Order: 1,
                    Configuration: Map(
                        ("PersonAttributeKey", "SponsorTier"),
                        ("PersonAttributeValue", "Gold")
                    )
                ),
                new WorkflowActionTypeSpec(
                    "add-group",
                    "Add to Sponsor Group",
                    "workflow.groups.add-member",
                    Order: 2,
                    Configuration: Map(("GroupId", groupId.ToString("D")))
                ),
                new WorkflowActionTypeSpec(
                    "issue-invoice",
                    "Issue Dues Invoice",
                    "workflow.finance.create-invoice",
                    Order: 3,
                    Configuration: Map(
                        ("FinancialAccountId", accountId.ToString("D")),
                        ("AmountAttributeKey", "PledgeAmount"),
                        ("Description", "Sponsor Annual Dues")
                    )
                ),
                new WorkflowActionTypeSpec(
                    "create-engagement",
                    "Create Welcome Opportunity Request",
                    "workflow.engagement.create-request",
                    Order: 4,
                    Configuration: Map(("OpportunityId", opportunityId.ToString("D")))
                ),
                new WorkflowActionTypeSpec(
                    "render-welcome",
                    "Render Welcome Merge Copy",
                    "workflow.templates.render-merge",
                    Order: 5,
                    Configuration: Map(
                        ("TargetAttributeKey", "RenderedWelcome"),
                        ("TemplateMarkup", "Welcome {{ FirstName }} ({{ WorkflowNumber }})")
                    )
                ),
                new WorkflowActionTypeSpec(
                    "send-notif",
                    "Queue Notification",
                    "workflow.comms.send-notification",
                    Order: 6,
                    Configuration: Map(("SystemCommunicationKey", "sponsor.welcome"))
                ),
                new WorkflowActionTypeSpec(
                    "post-webhook",
                    "Post Webhook",
                    "workflow.integrations.webhook-post",
                    Order: 7,
                    Configuration: Map(("Url", "https://hooks.example.org/workflows"))
                ),
                new WorkflowActionTypeSpec(
                    "route-high-value",
                    "Conditional Route",
                    "workflow.control.activate-activity",
                    Order: 8,
                    Configuration: Map(
                        ("SourceAttributeKey", "PledgeAmount"),
                        ("Operator", "greaterthan"),
                        ("ExpectedValue", "250"),
                        ("TargetActivityKey", "cooldown-and-finish")
                    )
                ),
            ]
        );
    }

    private static WorkflowActivityTypeSpec BuildCooldownActivity()
    {
        return new WorkflowActivityTypeSpec(
            "cooldown-and-finish",
            "Cooldown and Complete",
            Order: 1,
            Actions:
            [
                new WorkflowActionTypeSpec(
                    "inc-counter-2",
                    "Increment Again",
                    "workflow.attributes.set-value",
                    Order: 0,
                    Configuration: Map(("AttributeKey", "StepCounter"), ("IncrementBy", "1"))
                ),
                new WorkflowActionTypeSpec(
                    "delay-step",
                    "Delay 60s",
                    "workflow.control.set-status",
                    Order: 1,
                    Configuration: Map(
                        ("StateLabel", "Awaiting Settlement Window"),
                        ("DelaySeconds", "60")
                    )
                ),
                new WorkflowActionTypeSpec(
                    "complete-step",
                    "Mark Onboarded",
                    "workflow.control.set-status",
                    Order: 2,
                    Configuration: Map(("StateLabel", "Onboarded"), ("CompleteWorkflow", "true"))
                ),
            ]
        );
    }
}
