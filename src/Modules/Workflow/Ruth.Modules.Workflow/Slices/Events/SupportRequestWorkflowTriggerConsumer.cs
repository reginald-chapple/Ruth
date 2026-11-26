using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices.Events;

internal sealed class SupportRequestWorkflowTriggerConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<SupportRequestSubmittedIntegrationEvent>
{
    public const string EventKey = "engagement.support.submitted";

    public async Task ConsumeAsync(
        SupportRequestSubmittedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SupportRequestId"] = integrationEvent.SupportRequestId.ToString("D"),
            ["SupportCategoryId"] = integrationEvent.SupportCategoryId.ToString("D"),
            ["IsUrgent"] = integrationEvent.IsUrgent ? "true" : "false",
        };
        if (integrationEvent.RequesterPersonId.HasValue)
        {
            attrs["PersonId"] = integrationEvent.RequesterPersonId.Value.ToString("D");
        }

        _ = await commandDispatcher.SendAsync<
            DispatchWorkflowAutomationEventCommand,
            WorkflowAutomationDispatchResponse
        >(
            new DispatchWorkflowAutomationEventCommand(
                WorkflowTriggerKind.IntegrationEvent,
                EventKey,
                EntityTypeQualifier: integrationEvent.SupportCategoryId.ToString("D"),
                PrimaryEntityTypeKey: "Ruth.Engagement.SupportRequest",
                PrimaryEntityId: integrationEvent.SupportRequestId,
                InitiatorPersonId: integrationEvent.RequesterPersonId,
                PayloadAttributes: attrs
            ),
            cancellationToken
        );
    }
}
