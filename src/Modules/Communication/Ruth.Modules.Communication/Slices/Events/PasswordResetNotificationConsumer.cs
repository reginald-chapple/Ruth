using System.Globalization;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class PasswordResetNotificationConsumer(
    ICommandDispatcher commandDispatcher,
    ICanonicalTenantUrlProvider? canonicalUrlProvider = null
) : IIntegrationEventConsumer<PasswordResetRequestedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PasswordResetRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var resetPath =
            $"/reset-password?token={Uri.EscapeDataString(integrationEvent.ResetToken)}";
        var resetUrl = canonicalUrlProvider is not null
            ? await canonicalUrlProvider.BuildTenantUrlAsync(
                AmbientTenantContext.CurrentTenantId,
                resetPath,
                cancellationToken
            )
            : resetPath;

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = integrationEvent.UserName,
            ["ResetToken"] = integrationEvent.ResetToken,
            ["ResetUrl"] = resetUrl,
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
                "identity.password-reset",
                RecipientEmail: integrationEvent.Email,
                RecipientName: integrationEvent.UserName,
                MergeFields: merge,
                TriggerSource: "Identity.PasswordResetRequested"
            ),
            cancellationToken
        );
    }
}
