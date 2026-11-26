using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

[Collection(WorkflowPostgresFixtures.Name)]
public sealed class W5S6WorkflowFormsAndConsumersIntegrationTests(
    WorkflowPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task InteractiveForms_MyTasksInbox_ConditionalSubmission_AndPublicFormLaunch_WorkEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await WorkflowTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var reviewerId = Guid.CreateVersion7();
        var template = await SeedInteractiveGrantFormAsync(commands, reviewerId);
        await VerifyTaskAssignmentAndConditionalSubmitAsync(commands, queries, reviewerId);
        await VerifyPublicTemplateOneShotLaunchAsync(commands, template.Slug);
    }

    private static async Task<FormBuilderTemplateResponse> SeedInteractiveGrantFormAsync(
        ICommandDispatcher commands,
        Guid defaultTriageReviewerId
    )
    {
        var wfRes = await commands.SendAsync<CreateWorkflowTypeCommand, WorkflowTypeResponse>(
            new CreateWorkflowTypeCommand(
                new WorkflowTypeSpec(
                    "Grant Triage Workflow",
                    "grant-triage-workflow",
                    "GrantRequest",
                    "GRT-",
                    [
                        new WorkflowActivityTypeSpec(
                            "intake",
                            "Intake Entry",
                            Order: 0,
                            IsActivatedWithWorkflow: true,
                            Actions:
                            [
                                new WorkflowActionTypeSpec(
                                    "entry-form",
                                    "Applicant Form",
                                    "workflow.forms.user-entry",
                                    Order: 0,
                                    Configuration: WorkflowEngineTestSeedHelper.Map(
                                        ("AssignPersonId", defaultTriageReviewerId.ToString("D"))
                                    )
                                ),
                            ]
                        ),
                        new WorkflowActivityTypeSpec(
                            "final-approval",
                            "Final Approval",
                            Order: 1,
                            Actions:
                            [
                                new WorkflowActionTypeSpec(
                                    "finish-approved",
                                    "Mark Approved",
                                    "workflow.control.set-status",
                                    Order: 0,
                                    Configuration: WorkflowEngineTestSeedHelper.Map(
                                        ("StateLabel", "Approved"),
                                        ("CompleteWorkflow", "true")
                                    )
                                ),
                            ]
                        ),
                    ]
                )
            )
        );
        Assert.True(wfRes.IsSuccess);

        var templateRes = await commands.SendAsync<
            CreateFormBuilderTemplateCommand,
            FormBuilderTemplateResponse
        >(
            new CreateFormBuilderTemplateCommand(
                new FormBuilderTemplateSpec(
                    "Emergency Assistance Form",
                    "emergency-assistance-form",
                    ConfirmationTitle: "Assistance Request Logged",
                    LinkedWorkflowTypeId: wfRes.Value.Id
                )
            )
        );
        Assert.True(templateRes.IsSuccess);

        var actionTypeId = wfRes.Value.Activities[0].Actions[0].Id;
        var upsertForm = await commands.SendAsync<
            UpsertWorkflowActionFormCommand,
            WorkflowActionFormResponse
        >(
            new UpsertWorkflowActionFormCommand(
                actionTypeId,
                BuildInteractiveSpec(templateRes.Value.Id)
            )
        );
        Assert.True(upsertForm.IsSuccess);
        return templateRes.Value;
    }

    private static async Task VerifyTaskAssignmentAndConditionalSubmitAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid reviewerId
    )
    {
        var launched = await commands.SendAsync<LaunchWorkflowCommand, WorkflowInstanceResponse>(
            new LaunchWorkflowCommand("grant-triage-workflow", ProcessImmediately: true)
        );
        Assert.Equal(WorkflowInstanceStatus.WaitingForInput, launched.Value.Status);

        var openActId = launched.Value.Activities[0].Id;
        var assigned = await commands.SendAsync<
            AssignWorkflowActivityCommand,
            WorkflowInstanceResponse
        >(
            new AssignWorkflowActivityCommand(
                launched.Value.Id,
                openActId,
                AssignedPersonId: reviewerId,
                AssignedGroupId: null
            )
        );
        Assert.True(assigned.IsSuccess);

        var inbox = await queries.QueryAsync<
            GetMyWorkflowTasksQuery,
            IReadOnlyList<WorkflowTaskItemResponse>
        >(new GetMyWorkflowTasksQuery(AssignedPersonId: reviewerId, IncludeUnassigned: false));
        var task = Assert.Single(inbox.Value);
        Assert.Equal(launched.Value.Id, task.WorkflowId);

        var formView = await queries.QueryAsync<
            GetWorkflowEntryFormQuery,
            WorkflowEntryFormViewResponse
        >(new GetWorkflowEntryFormQuery(launched.Value.Id));
        Assert.True(formView.IsSuccess);

        var invalidGrantSubmit = await commands.SendAsync<
            SubmitWorkflowEntryFormCommand,
            WorkflowFormSubmissionResponse
        >(
            new SubmitWorkflowEntryFormCommand(
                launched.Value.Id,
                formView.Value.ActionId,
                "approve",
                WorkflowEngineTestSeedHelper.Map(("RequestKind", "Grant")),
                ActorPersonId: reviewerId
            )
        );
        Assert.True(invalidGrantSubmit.IsFailure);

        var validGrantSubmit = await commands.SendAsync<
            SubmitWorkflowEntryFormCommand,
            WorkflowFormSubmissionResponse
        >(
            new SubmitWorkflowEntryFormCommand(
                launched.Value.Id,
                formView.Value.ActionId,
                "approve",
                WorkflowEngineTestSeedHelper.Map(
                    ("RequestKind", "Grant"),
                    ("GrantJustification", "Community pantry refrigeration repair")
                ),
                ActorPersonId: reviewerId
            )
        );
        Assert.True(validGrantSubmit.IsSuccess);
        Assert.Equal(WorkflowInstanceStatus.Completed, validGrantSubmit.Value.Workflow.Status);
        Assert.Equal("Approved", validGrantSubmit.Value.Workflow.StateLabel);
    }

    private static async Task VerifyPublicTemplateOneShotLaunchAsync(
        ICommandDispatcher commands,
        string templateSlug
    )
    {
        var oneShotPublic = await commands.SendAsync<
            LaunchFormBuilderSubmissionCommand,
            WorkflowFormSubmissionResponse
        >(
            new LaunchFormBuilderSubmissionCommand(
                templateSlug,
                SelectedButtonKey: "approve",
                FieldValues: WorkflowEngineTestSeedHelper.Map(("RequestKind", "General")),
                SubmitterFirstName: "Jordan",
                SubmitterLastName: "Rivers",
                SubmitterEmail: "jordan.rivers@example.org"
            )
        );
        Assert.True(oneShotPublic.IsSuccess);
        Assert.Equal(WorkflowInstanceStatus.Completed, oneShotPublic.Value.Workflow.Status);
    }

    private static WorkflowActionFormSpec BuildInteractiveSpec(Guid templateId)
    {
        return new WorkflowActionFormSpec(
            Fields:
            [
                new WorkflowActionFormFieldSpec(
                    "RequestKind",
                    "Request Kind",
                    "select",
                    Order: 0,
                    SectionKey: "main",
                    IsRequired: true,
                    DefaultValue: "General",
                    SelectOptions: ["General", "Grant"]
                ),
                new WorkflowActionFormFieldSpec(
                    "GrantJustification",
                    "Grant Justification",
                    "memo",
                    Order: 1,
                    SectionKey: "main",
                    IsRequired: true,
                    VisibilityRule: new ConditionalVisibilityRuleDto(
                        VisibilityMatchMode.All,
                        [
                            new FieldVisibilityConditionDto(
                                "RequestKind",
                                FieldVisibilityOperator.Equals,
                                "Grant"
                            ),
                        ]
                    )
                ),
            ],
            Buttons:
            [
                new WorkflowFormButtonSpec(
                    "approve",
                    "Approve",
                    TargetActivityKey: "final-approval",
                    SetStateLabel: "Approved",
                    Order: 0
                ),
            ],
            Sections: [new WorkflowActionFormSectionSpec("main", "Details", Order: 0)],
            FormBuilderTemplateId: templateId,
            AllowPersonEntry: true
        );
    }
}
