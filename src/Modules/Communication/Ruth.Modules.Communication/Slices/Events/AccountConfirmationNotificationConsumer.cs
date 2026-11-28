using System.Globalization;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class AccountConfirmationNotificationConsumer(
    ICommandDispatcher commandDispatcher,
    ICanonicalTenantUrlProvider? canonicalUrlProvider = null
) : IIntegrationEventConsumer<AccountConfirmationRequestedIntegrationEvent>
{
    public async Task ConsumeAsync(
        AccountConfirmationRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var confirmPath =
            $"/confirm-account?token={Uri.EscapeDataString(integrationEvent.ConfirmationToken)}";
        var confirmUrl = canonicalUrlProvider is not null
            ? await canonicalUrlProvider.BuildTenantUrlAsync(
                AmbientTenantContext.CurrentTenantId,
                confirmPath,
                cancellationToken
            )
            : confirmPath;

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = integrationEvent.UserName,
            ["ConfirmationToken"] = integrationEvent.ConfirmationToken,
            ["ConfirmationUrl"] = confirmUrl,
            ["ExpiresOnUtc"] = integrationEvent.ExpiresOnUtc.ToString(
                "u",
                CultureInfo.InvariantCulture
            ),
        };

        _ = await commandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                "identity.account-confirmation",
                RecipientEmail: integrationEvent.Email,
                RecipientName: integrationEvent.UserName,
                MergeFields: merge,
                TriggerSource: "Identity.AccountConfirmationRequested"
            ),
            cancellationToken
        );
    }
}
