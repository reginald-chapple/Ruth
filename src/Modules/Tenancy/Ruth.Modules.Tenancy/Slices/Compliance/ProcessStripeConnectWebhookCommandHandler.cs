using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal sealed class ProcessStripeConnectWebhookCommandHandler(
    TenancyDbContext dbContext,
    IOptions<TenancyOptions> options,
    WebhookReplayDeduplicator replayDeduplicator,
    IClock clock
) : ICommandHandler<ProcessStripeConnectWebhookCommand, StripeConnectWebhookResponse>
{
    public async Task<Result<StripeConnectWebhookResponse>> HandleAsync(
        ProcessStripeConnectWebhookCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = clock.UtcNow;

        var gateError = VerifyAndRegisterDelivery(command, now, out var parsed);
        return gateError is not null
            ? gateError
            : await ApplyCapabilitiesAsync(parsed, now, cancellationToken);
    }

    private DomainError? VerifyAndRegisterDelivery(
        ProcessStripeConnectWebhookCommand command,
        DateTimeOffset now,
        out ParsedConnectEvent parsed
    )
    {
        parsed = default;
        var tolerance = TimeSpan.FromSeconds(
            Math.Clamp(options.Value.WebhookReplayToleranceSeconds, 30, 3600)
        );

        if (
            !WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                command.RawPayload,
                command.StripeSignatureHeader,
                options.Value.StripeConnectWebhookSecret,
                now,
                signatureKey: "v1",
                tolerance: tolerance
            )
        )
        {
            return TenancyErrors.StripeConnectWebhookUnauthorized;
        }

        var isParsed = TryParseAccountUpdatedPayload(command.RawPayload, out parsed);
        return !isParsed ? TenancyErrors.StripeConnectWebhookMalformed
            : !replayDeduplicator.TryRegisterDelivery(
                "stripe-connect",
                parsed.EventId,
                now,
                tolerance
            )
                ? TenancyErrors.StripeConnectWebhookReplayed
            : null;
    }

    private async Task<Result<StripeConnectWebhookResponse>> ApplyCapabilitiesAsync(
        ParsedConnectEvent parsed,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var account = await dbContext.StripeConnectAccounts.FirstOrDefaultAsync(
            a => a.ExternalAccountId == parsed.AccountId,
            cancellationToken
        );
        if (account is null)
        {
            return TenancyErrors.StripeConnectAccountNotFound;
        }

        var ambientTenantId = AmbientTenantContext.CurrentTenantId;
        if (!ambientTenantId.IsEmpty && account.TargetTenantId != ambientTenantId)
        {
            return TenancyErrors.StripeConnectAccountNotFound;
        }

        account.SynchronizeCapabilities(
            parsed.ChargesEnabled,
            parsed.PayoutsEnabled,
            parsed.DetailsSubmitted,
            now
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new StripeConnectWebhookResponse(
            parsed.EventId,
            parsed.EventType,
            account.ExternalAccountId,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted
        );
    }

    private static bool TryParseAccountUpdatedPayload(
        string rawPayload,
        out ParsedConnectEvent parsed
    )
    {
        parsed = default;
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;
            var eventId = TryGetString(root, "id");
            var eventType = TryGetString(root, "type");
            if (
                string.IsNullOrWhiteSpace(eventId)
                || string.IsNullOrWhiteSpace(eventType)
                || !root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj)
            )
            {
                return false;
            }

            var accountId = TryGetString(obj, "id") ?? TryGetString(root, "account");
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return false;
            }

            var charges = TryGetBool(obj, "charges_enabled");
            var payouts = TryGetBool(obj, "payouts_enabled");
            var details = TryGetBool(obj, "details_submitted");
            parsed = new ParsedConnectEvent(
                eventId.Trim(),
                eventType.Trim(),
                accountId.Trim(),
                charges,
                payouts,
                details
            );
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryGetString(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private static bool TryGetBool(JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.True;
    }

    private readonly record struct ParsedConnectEvent(
        string EventId,
        string EventType,
        string AccountId,
        bool ChargesEnabled,
        bool PayoutsEnabled,
        bool DetailsSubmitted
    );
}
