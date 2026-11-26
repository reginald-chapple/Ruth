using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices.Events;

internal sealed class EngagementConnectedWorkflowTriggerConsumer(
    ICommandDispatcher commandDispatcher
) : IIntegrationEventConsumer<EngagementRequestConnectedIntegrationEvent>
{
    public const string EventKey = "engagement.request.connected";

    public async Task ConsumeAsync(
        EngagementRequestConnectedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EngagementRequestId"] = integrationEvent.EngagementRequestId.ToString("D"),
            ["PipelineTypeId"] = integrationEvent.PipelineTypeId.ToString("D"),
            ["OpportunityId"] = integrationEvent.EngagementOpportunityId.ToString("D"),
            ["PersonId"] = integrationEvent.RequesterPersonId.ToString("D"),
        };
        if (integrationEvent.PlacementGroupId.HasValue)
        {
            attrs["GroupId"] = integrationEvent.PlacementGroupId.Value.ToString("D");
        }

        _ = await commandDispatcher.SendAsync<
            DispatchWorkflowAutomationEventCommand,
            WorkflowAutomationDispatchResponse
        >(
            new DispatchWorkflowAutomationEventCommand(
                WorkflowTriggerKind.IntegrationEvent,
                EventKey,
                EntityTypeQualifier: integrationEvent.PipelineTypeId.ToString("D"),
                PrimaryEntityTypeKey: "Ruth.Engagement.EngagementRequest",
                PrimaryEntityId: integrationEvent.EngagementRequestId,
                InitiatorPersonId: integrationEvent.RequesterPersonId,
                PayloadAttributes: attrs
            ),
            cancellationToken
        );
    }
}
