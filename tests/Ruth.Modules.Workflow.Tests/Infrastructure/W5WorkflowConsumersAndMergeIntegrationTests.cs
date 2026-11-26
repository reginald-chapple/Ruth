using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

[Collection(WorkflowPostgresFixtures.Name)]
public sealed class W5WorkflowConsumersAndMergeIntegrationTests(
    WorkflowPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task AutomationTriggers_IntegrationEventConsumers_AndPersonMerge_WorkIdempotently()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await WorkflowTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var commands = sp.GetRequiredService<ICommandDispatcher>();
        var queries = sp.GetRequiredService<IQueryDispatcher>();

        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();
        var autoTypeId = await SeedAutoFollowupWorkflowAndTriggersAsync(commands, retiredPersonId);

        await FireCrossModuleStimuliAsync(sp, retiredPersonId);
        await FirePersonMergeIdempotentlyAsync(sp, survivingPersonId, retiredPersonId);

        var instances = await queries.QueryAsync<
            GetWorkflowInstancesQuery,
            IReadOnlyList<WorkflowInstanceResponse>
        >(new GetWorkflowInstancesQuery(autoTypeId, InitiatorPersonId: survivingPersonId));
        Assert.Equal(4, instances.Value.Count);
        Assert.All(instances.Value, static i =>
            Assert.Equal(WorkflowInstanceStatus.Completed, i.Status)
        );
    }

    private static async Task<Guid> SeedAutoFollowupWorkflowAndTriggersAsync(
        ICommandDispatcher commands,
        Guid retiredPersonId
    )
    {
        var autoType = await commands.SendAsync<CreateWorkflowTypeCommand, WorkflowTypeResponse>(
            new CreateWorkflowTypeCommand(
                new WorkflowTypeSpec(
                    "Auto Care Follow-Up",
                    "auto-care-followup",
                    "FollowUp",
                    "CAR-",
                    [
                        new WorkflowActivityTypeSpec(
                            "finish",
                            "Auto Complete",
                            Order: 0,
                            IsActivatedWithWorkflow: true,
                            DefaultAssignedPersonId: retiredPersonId,
                            Actions:
                            [
                                new WorkflowActionTypeSpec(
                                    "done",
                                    "Complete",
                                    "workflow.control.set-status",
                                    Order: 0,
                                    Configuration: WorkflowEngineTestSeedHelper.Map(
                                        ("StateLabel", "Triaged"),
                                        ("CompleteWorkflow", "true")
                                    )
                                ),
                            ]
                        ),
                    ]
                )
            )
        );
        Assert.True(autoType.IsSuccess);

        foreach (
            var eventKey in new[]
            {
                "engagement.support.submitted",
                "engagement.request.connected",
                "commerce.booking.confirmed",
                "finance.invoice.settled",
            }
        )
        {
            var trig = await commands.SendAsync<
                CreateWorkflowTriggerCommand,
                WorkflowTriggerResponse
            >(
                new CreateWorkflowTriggerCommand(
                    new WorkflowTriggerSpec(
                        $"Trigger {eventKey}",
                        WorkflowTriggerKind.IntegrationEvent,
                        eventKey,
                        autoType.Value.Id,
                        WorkflowNameTemplate: "Auto Follow-Up for {{ PersonId }}"
                    )
                )
            );
            Assert.True(trig.IsSuccess);
        }

        return autoType.Value.Id;
    }

    private static async Task FireCrossModuleStimuliAsync(IServiceProvider sp, Guid retiredPersonId)
    {
        var now = DateTimeOffset.UtcNow;

        await sp.GetRequiredService<
            IIntegrationEventConsumer<SupportRequestSubmittedIntegrationEvent>
        >()
            .ConsumeAsync(
                new SupportRequestSubmittedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOnUtc: now,
                    SupportRequestId: Guid.CreateVersion7(),
                    SupportCategoryId: Guid.CreateVersion7(),
                    RequesterPersonId: retiredPersonId,
                    BranchId: null,
                    IsUrgent: true
                )
            );

        await sp.GetRequiredService<
            IIntegrationEventConsumer<EngagementRequestConnectedIntegrationEvent>
        >()
            .ConsumeAsync(
                new EngagementRequestConnectedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOnUtc: now,
                    EngagementRequestId: Guid.CreateVersion7(),
                    EngagementOpportunityId: Guid.CreateVersion7(),
                    PipelineTypeId: Guid.CreateVersion7(),
                    RequesterPersonId: retiredPersonId,
                    AssignedConnectorPersonId: null,
                    PlacementGroupId: null
                )
            );

        await sp.GetRequiredService<
            IIntegrationEventConsumer<ServiceBookingConfirmedIntegrationEvent>
        >()
            .ConsumeAsync(
                new ServiceBookingConfirmedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOnUtc: now,
                    BookingId: Guid.CreateVersion7(),
                    ConfirmationCode: "BK-1001",
                    TargetKind: BookingTargetKind.Service,
                    BookableServiceId: Guid.CreateVersion7(),
                    RentableResourceId: null,
                    TargetName: "Remote Consult",
                    DeliveryMode: BookingDeliveryMode.Remote,
                    CustomerPersonId: retiredPersonId,
                    CustomerName: "Casey Member",
                    CustomerEmail: "casey@example.org",
                    ProviderPersonId: null,
                    LocationId: null,
                    BranchId: null,
                    StartUtc: now.AddHours(1),
                    EndUtc: now.AddHours(2),
                    ParticipantOrUnitCount: 1,
                    CurrencyCode: "USD",
                    TotalAmount: 50m,
                    PaidAmount: 50m,
                    LinkedInvoiceId: null
                )
            );

        await sp.GetRequiredService<IIntegrationEventConsumer<InvoiceSettledIntegrationEvent>>()
            .ConsumeAsync(
                new InvoiceSettledIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOnUtc: now,
                    InvoiceId: Guid.CreateVersion7(),
                    InvoiceNumber: "INV-00100",
                    PersonId: retiredPersonId,
                    TotalPaidAmount: 125.00m,
                    CurrencyCode: "USD"
                )
            );
    }

    private static async Task FirePersonMergeIdempotentlyAsync(
        IServiceProvider sp,
        Guid survivingPersonId,
        Guid retiredPersonId
    )
    {
        var mergeConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        var mergeEvt = new PersonMergedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            survivingPersonId,
            retiredPersonId,
            [],
            HadMultipleDistinctEmails: false
        );
        await mergeConsumer.ConsumeAsync(mergeEvt);
        await mergeConsumer.ConsumeAsync(mergeEvt);
    }
}
