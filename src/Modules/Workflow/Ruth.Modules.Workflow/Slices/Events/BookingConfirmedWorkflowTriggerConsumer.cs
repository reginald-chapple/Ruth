using System.Globalization;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices.Events;

internal sealed class BookingConfirmedWorkflowTriggerConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<ServiceBookingConfirmedIntegrationEvent>
{
    public const string EventKey = "commerce.booking.confirmed";

    public async Task ConsumeAsync(
        ServiceBookingConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["BookingId"] = integrationEvent.BookingId.ToString("D"),
            ["ConfirmationCode"] = integrationEvent.ConfirmationCode,
            ["TargetName"] = integrationEvent.TargetName,
            ["DeliveryMode"] = integrationEvent.DeliveryMode.ToString(),
            ["CustomerName"] = integrationEvent.CustomerName,
            ["Email"] = integrationEvent.CustomerEmail,
            ["TotalAmount"] = integrationEvent.TotalAmount.ToString(CultureInfo.InvariantCulture),
        };
        if (integrationEvent.CustomerPersonId.HasValue)
        {
            attrs["PersonId"] = integrationEvent.CustomerPersonId.Value.ToString("D");
        }

        _ = await commandDispatcher.SendAsync<
            DispatchWorkflowAutomationEventCommand,
            WorkflowAutomationDispatchResponse
        >(
            new DispatchWorkflowAutomationEventCommand(
                WorkflowTriggerKind.IntegrationEvent,
                EventKey,
                EntityTypeQualifier: integrationEvent.TargetKind.ToString(),
                PrimaryEntityTypeKey: "Ruth.Commerce.ServiceBooking",
                PrimaryEntityId: integrationEvent.BookingId,
                InitiatorPersonId: integrationEvent.CustomerPersonId,
                PayloadAttributes: attrs
            ),
            cancellationToken
        );
    }
}
