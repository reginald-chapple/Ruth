using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Modular provider contract for external background screening integrations (e.g., Checkr, Sterling).</summary>
public interface IBackgroundCheckProvider
{
    /// <summary>Gets the unique lowercase provider key (e.g., "checkr", "in-memory").</summary>
    string ProviderKey { get; }

    /// <summary>Gets the human-readable provider name.</summary>
    string DisplayName { get; }

    /// <summary>Gets the HTTP header name carrying the webhook HMAC signature (e.g., X-Checkr-Signature).</summary>
    string WebhookSignatureHeaderName { get; }

    /// <summary>Lists available screening packages from this provider.</summary>
    Task<Result<IReadOnlyList<BackgroundCheckPackageResponse>>> GetPackagesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Submits a new background check request for a candidate.</summary>
    Task<Result<BackgroundCheckSubmissionResponse>> SubmitRequestAsync(
        BackgroundCheckSubmissionRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Verifies an inbound webhook request signature using constant-time HMAC verification.</summary>
    bool VerifyWebhookSignature(string rawPayload, string? signatureHeader);

    /// <summary>Parses a verified raw webhook JSON payload into a normalized event.</summary>
    Result<BackgroundCheckWebhookEvent> ParseWebhookEvent(string rawPayload);
}
