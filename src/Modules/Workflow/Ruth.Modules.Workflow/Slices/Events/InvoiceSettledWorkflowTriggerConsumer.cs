using System.Globalization;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Slices.Events;

internal sealed class InvoiceSettledWorkflowTriggerConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<InvoiceSettledIntegrationEvent>
{
    public const string EventKey = "finance.invoice.settled";

    public async Task ConsumeAsync(
        InvoiceSettledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["InvoiceId"] = integrationEvent.InvoiceId.ToString("D"),
            ["InvoiceNumber"] = integrationEvent.InvoiceNumber,
            ["PersonId"] = integrationEvent.PersonId.ToString("D"),
            ["TotalPaidAmount"] = integrationEvent.TotalPaidAmount.ToString(
                CultureInfo.InvariantCulture
            ),
            ["CurrencyCode"] = integrationEvent.CurrencyCode,
        };

        _ = await commandDispatcher.SendAsync<
            DispatchWorkflowAutomationEventCommand,
            WorkflowAutomationDispatchResponse
        >(
            new DispatchWorkflowAutomationEventCommand(
                WorkflowTriggerKind.IntegrationEvent,
                EventKey,
                EntityTypeQualifier: integrationEvent.CurrencyCode,
                PrimaryEntityTypeKey: "Ruth.Finance.Invoice",
                PrimaryEntityId: integrationEvent.InvoiceId,
                InitiatorPersonId: integrationEvent.PersonId,
                PayloadAttributes: attrs
            ),
            cancellationToken
        );
    }
}
