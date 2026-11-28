using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class CommunicationInboundWebhookGuard(
    IOptions<CommunicationBulkOptions> bulkOptions,
    WebhookReplayDeduplicator replayDeduplicator,
    IClock clock
)
{
    private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    public async Task<Result<TPayload>> ReadAndVerifyAsync<TPayload>(
        HttpContext httpContext,
        string providerScope,
        DomainError emptyBodyError,
        CancellationToken cancellationToken
    )
        where TPayload : class
    {
        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signature = httpContext
            .Request.Headers[CommunicationWebhookSignatureVerifier.HeaderName]
            .FirstOrDefault();
        var timestamp = httpContext
            .Request.Headers[CommunicationWebhookSignatureVerifier.TimestampHeaderName]
            .FirstOrDefault();
        var tolerance = TimeSpan.FromSeconds(
            Math.Clamp(bulkOptions.Value.WebhookReplayToleranceSeconds, 30, 3600)
        );
        var now = clock.UtcNow;

        if (
            !CommunicationWebhookSignatureVerifier.VerifyTimestamped(
                rawBody,
                signature,
                timestamp,
                bulkOptions.Value.WebhookSigningSecret,
                now,
                tolerance
            )
        )
        {
            return Result.Failure<TPayload>(BulkCommunicationErrors.WebhookSignatureInvalid);
        }

        var bodyDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));
        var tenantKey = AmbientTenantContext.CurrentTenantId.Value.ToString("N");
        var replayKey = $"{tenantKey}:{signature}:{bodyDigest}";
        if (!replayDeduplicator.TryRegisterDelivery(providerScope, replayKey, now, tolerance))
        {
            return Result.Failure<TPayload>(BulkCommunicationErrors.WebhookReplayed);
        }

        var parsed = string.IsNullOrWhiteSpace(rawBody)
            ? null
            : JsonSerializer.Deserialize<TPayload>(rawBody, _jsonOptions);
        return parsed is null ? Result.Failure<TPayload>(emptyBodyError) : Result.Success(parsed);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }
}
