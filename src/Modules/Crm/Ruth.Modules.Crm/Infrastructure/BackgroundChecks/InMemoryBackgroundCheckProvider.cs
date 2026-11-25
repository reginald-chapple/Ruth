using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Infrastructure.BackgroundChecks;

/// <summary>Built-in development and test background check provider requiring no external credentials.</summary>
public sealed class InMemoryBackgroundCheckProvider : IBackgroundCheckProvider
{
    /// <summary>Provider key.</summary>
    public const string Key = "in-memory";

    /// <summary>Default local webhook signing secret used for test HMAC validation.</summary>
    public const string DefaultWebhookSecret = "ruth-in-memory-webhook-secret";

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public string DisplayName => "In-Memory Development Provider";

    /// <inheritdoc />
    public string WebhookSignatureHeaderName => "X-Ruth-Signature";

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<BackgroundCheckPackageResponse>>> GetPackagesAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<BackgroundCheckPackageResponse> packages =
        [
            new BackgroundCheckPackageResponse(
                Key,
                "standard-volunteer",
                "Standard Volunteer Screening",
                "Local simulated background check package for development and testing."
            ),
            new BackgroundCheckPackageResponse(
                Key,
                "youth-vulnerable-enhanced",
                "Youth & Vulnerable Populations Enhanced",
                "Simulated enhanced multi-jurisdiction screening package."
            ),
        ];
        return Task.FromResult(Result.Success(packages));
    }

    /// <inheritdoc />
    public Task<Result<BackgroundCheckSubmissionResponse>> SubmitRequestAsync(
        BackgroundCheckSubmissionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var providerRequestId = $"sim_{request.BackgroundCheckId:N}";
        return Task.FromResult(
            Result.Success(
                new BackgroundCheckSubmissionResponse(
                    providerRequestId,
                    BackgroundCheckStatus.InvitationSent,
                    "invitation.created",
                    $"https://localhost/simulated-check/{providerRequestId}"
                )
            )
        );
    }

    /// <inheritdoc />
    public bool VerifyWebhookSignature(string rawPayload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(rawPayload) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var expected = ComputeSignature(rawPayload);
        var left = Encoding.UTF8.GetBytes(expected);
        var right = Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant());
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    /// <inheritdoc />
    public Result<BackgroundCheckWebhookEvent> ParseWebhookEvent(string rawPayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;
            var reqId = root.GetProperty("requestId").GetString() ?? string.Empty;
            var statusStr = root.GetProperty("status").GetString() ?? "pending";
            var responseId = root.TryGetProperty("responseId", out var r) ? r.GetString() : null;
            var (status, recordFound, rec) = MapStatus(statusStr);

            return Result.Success(
                new BackgroundCheckWebhookEvent(
                    Guid.CreateVersion7().ToString("D"),
                    $"report.{statusStr}",
                    reqId,
                    responseId,
                    PackageName: null,
                    status,
                    recordFound,
                    rec,
                    rawPayload
                )
            );
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            return DomainError.Validation("Crm.Webhook.Invalid", "Invalid simulated webhook JSON.");
        }
    }

    private static (
        BackgroundCheckStatus Status,
        bool? RecordFound,
        string Recommendation
    ) MapStatus(string statusStr)
    {
        return statusStr.ToLowerInvariant() switch
        {
            "clear" => (BackgroundCheckStatus.ReportClear, false, "Candidate Pass"),
            "consider" => (BackgroundCheckStatus.ReportConsider, true, "Candidate Review"),
            _ => (BackgroundCheckStatus.ReportPending, null, "Report Pending"),
        };
    }

    /// <summary>Computes the expected HMAC-SHA256 hex digest for simulated webhook tests.</summary>
    public static string ComputeSignature(string rawPayload)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(DefaultWebhookSecret),
            Encoding.UTF8.GetBytes(rawPayload)
        );
        return Convert.ToHexStringLower(hash);
    }
}
