using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class OrganizationOnboardedNotificationConsumer(
    ICommandDispatcher commandDispatcher,
    IOptions<CommunicationTransportOptions> options
) : IIntegrationEventConsumer<OrganizationOnboardedIntegrationEvent>
{
    public async Task ConsumeAsync(
        OrganizationOnboardedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrganizationName"] = integrationEvent.OrganizationName,
            ["TimeZoneId"] = integrationEvent.TimeZoneId,
        };

        _ = await commandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                "identity.organization-onboarded",
                RecipientEmail: options.Value.DefaultFromEmail,
                RecipientName: integrationEvent.OrganizationName,
                MergeFields: merge,
                TriggerSource: "Identity.OrganizationOnboarded"
            ),
            cancellationToken
        );
    }
}
