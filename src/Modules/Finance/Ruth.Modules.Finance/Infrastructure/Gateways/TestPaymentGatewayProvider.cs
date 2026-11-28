using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Infrastructure.Gateways;

internal sealed class TestPaymentGatewayProvider(
    IOptions<FinanceSecurityOptions> securityOptions,
    IClock clock
) : IPaymentGateway
{
    public const string CanonicalProviderKey = "test";

    public string ProviderKey => CanonicalProviderKey;

    public string DisplayName => "Ruth Deterministic Test & Sandbox Gateway";

    public Task<Result<GatewayCustomerVaultResult>> VaultPaymentMethodAsync(
        GatewayVaultRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (
            string.IsNullOrWhiteSpace(request.HostedPaymentToken)
            || request.HostedPaymentToken.StartsWith(
                "tok_decline",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return Task.FromResult(
                Result.Failure<GatewayCustomerVaultResult>(
                    DomainError.Failure(
                        "Finance.Gateway.TokenDeclined",
                        "The hosted payment token was declined by the test gateway."
                    )
                )
            );
        }

        var tokenSuffix = Guid.CreateVersion7().ToString("N")[..12];
        var vaultToken = $"tvault_{tokenSuffix}";
        var last4 = ExtractSafeLast4(request.MaskedLast4Hint, request.MethodKind);
        var brand =
            !string.IsNullOrWhiteSpace(request.CardBrandHint) ? request.CardBrandHint.Trim()
            : request.MethodKind == PaymentMethodKind.AchBankAccount ? "ACH Checking"
            : "Visa";
        var expMonth =
            request.MethodKind == PaymentMethodKind.CreditCard
                ? (request.ExpiryMonthHint ?? 12)
                : (int?)null;
        var expYear =
            request.MethodKind == PaymentMethodKind.CreditCard
                ? (request.ExpiryYearHint ?? (clock.UtcNow.Year + 3))
                : (int?)null;

        return Task.FromResult(
            Result.Success(
                new GatewayCustomerVaultResult(vaultToken, $"****{last4}", brand, expMonth, expYear)
            )
        );
    }

    public Task<Result<GatewayChargeResult>> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var sourceToken = request.CustomerVaultToken ?? request.OneTimeHostedToken ?? string.Empty;
        if (
            request.Amount <= 0m
            || string.IsNullOrWhiteSpace(sourceToken)
            || sourceToken.Contains("decline", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Task.FromResult(
                Result.Failure<GatewayChargeResult>(
                    DomainError.Failure(
                        "Finance.Gateway.ChargeDeclined",
                        "Payment charge was declined by the test gateway."
                    )
                )
            );
        }

        var txId = $"ttx_{Guid.CreateVersion7():N}";
        var authCode = $"AUTH{txId[^6..].ToUpperInvariant()}";
        return Task.FromResult(
            Result.Success(
                new GatewayChargeResult(txId, authCode, IsSettledImmediately: true, clock.UtcNow)
            )
        );
    }

    public Task<Result<GatewayRefundResult>> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m || string.IsNullOrWhiteSpace(request.OriginalGatewayTransactionId))
        {
            return Task.FromResult(
                Result.Failure<GatewayRefundResult>(
                    DomainError.Failure(
                        "Finance.Gateway.RefundFailed",
                        "Refund request requires a positive amount and original gateway transaction ID."
                    )
                )
            );
        }

        var refundId = $"tref_{Guid.CreateVersion7():N}";
        return Task.FromResult(Result.Success(new GatewayRefundResult(refundId, clock.UtcNow)));
    }

    public Task<Result<GatewaySubscriptionSyncResult>> SyncSubscriptionAsync(
        GatewaySubscriptionSyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var subRef = string.IsNullOrWhiteSpace(request.ExistingSubscriptionReference)
            ? $"tsub_{request.ScheduleId:N}"
            : request.ExistingSubscriptionReference;
        return Task.FromResult(
            Result.Success(new GatewaySubscriptionSyncResult(subRef, clock.UtcNow))
        );
    }

    public Result<GatewayWebhookPayload> ParseAndVerifyWebhook(
        string rawBody,
        string? signatureHeader
    )
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return DomainError.Unauthorized(
                "Finance.Gateway.WebhookSignatureMissing",
                "Webhook body and HMAC signature header are required."
            );
        }

        var secret = securityOptions.Value.TestGatewayWebhookSecret;
        var provided = signatureHeader.Trim();
        bool verified;
        if (provided.StartsWith("t=", StringComparison.Ordinal))
        {
            verified =
                WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                    rawBody,
                    provided,
                    secret,
                    clock.UtcNow,
                    signatureKey: "v1"
                )
                || WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                    rawBody,
                    provided,
                    secret,
                    clock.UtcNow,
                    signatureKey: "sha256"
                );
        }
        else
        {
            var hex = provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                ? provided.AsSpan(7)
                : provided.AsSpan();
            verified = WebhookHmacReplayVerifier.VerifyRawHmacHexConstantTime(rawBody, hex, secret);
        }

        return !verified
            ? DomainError.Unauthorized(
                "Finance.Gateway.WebhookSignatureInvalid",
                "Test gateway webhook HMAC-SHA256 or replay timestamp verification failed."
            )
            : DeserializeWebhookJson(rawBody, clock.UtcNow);
    }

    private static Result<GatewayWebhookPayload> DeserializeWebhookJson(
        string rawBody,
        DateTimeOffset fallbackNow
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var eventId = root.GetProperty("eventId").GetString();
            var kindText = root.GetProperty("eventKind").GetString();
            if (
                string.IsNullOrWhiteSpace(eventId)
                || !Enum.TryParse<GatewayWebhookEventKind>(kindText, ignoreCase: true, out var kind)
            )
            {
                return DomainError.Validation(
                    "Finance.Gateway.WebhookPayloadInvalid",
                    "Webhook JSON is missing eventId or a valid eventKind."
                );
            }

            var subRef = TryReadString(root, "subscriptionReference");
            var txRef = TryReadString(root, "transactionId");
            var currency = TryReadString(root, "currencyCode");
            var failure = TryReadString(root, "failureMessage");
            var amount =
                root.TryGetProperty("amount", out var amtEl)
                && amtEl.ValueKind == JsonValueKind.Number
                    ? amtEl.GetDecimal()
                    : (decimal?)null;

            return new GatewayWebhookPayload(
                eventId,
                kind,
                subRef,
                txRef,
                amount,
                currency,
                failure,
                fallbackNow
            );
        }
        catch (JsonException)
        {
            return DomainError.Validation(
                "Finance.Gateway.WebhookPayloadInvalid",
                "Webhook body is not valid JSON."
            );
        }
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    private static string ExtractSafeLast4(string? hint, PaymentMethodKind kind)
    {
        if (!string.IsNullOrWhiteSpace(hint))
        {
            var digits = new string([.. hint.Where(char.IsAsciiDigit)]);
            if (digits.Length >= 4)
            {
                return digits[^4..];
            }
        }

        return kind == PaymentMethodKind.AchBankAccount ? "6789" : "4242";
    }
}
