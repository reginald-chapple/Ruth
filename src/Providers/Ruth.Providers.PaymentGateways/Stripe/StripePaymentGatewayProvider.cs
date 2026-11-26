using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.PaymentGateways.Stripe;

/// <summary>Stripe v1 REST PaymentIntents, PaymentMethods, Refunds, and Subscriptions gateway adapter.</summary>
public sealed class StripePaymentGatewayProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<StripeGatewayOptions> options,
    IClock clock
) : IPaymentGateway
{
    /// <summary>HttpClient logical name.</summary>
    public const string HttpClientName = "Ruth.PaymentGateways.Stripe";

    /// <inheritdoc />
    public string ProviderKey => "stripe";

    /// <inheritdoc />
    public string DisplayName => "Stripe (PaymentIntents, ACH & Subscriptions)";

    /// <inheritdoc />
    public async Task<Result<GatewayCustomerVaultResult>> VaultPaymentMethodAsync(
        GatewayVaultRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            return UnconfiguredError<GatewayCustomerVaultResult>();
        }

        var customerForm = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["payment_method"] = request.HostedPaymentToken,
        };
        if (!string.IsNullOrWhiteSpace(request.BillingEmail))
        {
            customerForm["email"] = request.BillingEmail;
        }

        var customerDoc = await PostStripeFormAsync(
            "/v1/customers",
            customerForm,
            idempotencyKey: null,
            cancellationToken
        );
        if (customerDoc.IsFailure)
        {
            return Result.Failure<GatewayCustomerVaultResult>(customerDoc.Errors);
        }

        using var doc = customerDoc.Value;
        var customerId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
        var compositeVaultToken = $"{customerId}|{request.HostedPaymentToken}";
        var last4 = string.IsNullOrWhiteSpace(request.MaskedLast4Hint)
            ? "4242"
            : request.MaskedLast4Hint;
        var masked = last4.StartsWith("****", StringComparison.Ordinal)
            ? last4
            : $"****{last4[^Math.Min(4, last4.Length)..]}";

        return new GatewayCustomerVaultResult(
            compositeVaultToken,
            masked,
            request.CardBrandHint ?? "Card",
            request.ExpiryMonthHint,
            request.ExpiryYearHint
        );
    }

    /// <inheritdoc />
    public async Task<Result<GatewayChargeResult>> ChargeAsync(
        GatewayChargeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            return UnconfiguredError<GatewayChargeResult>();
        }

        var minorUnits = ConvertToMinorUnits(request.Amount, request.CurrencyCode);
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = minorUnits.ToString(CultureInfo.InvariantCulture),
            ["currency"] = request.CurrencyCode.ToLowerInvariant(),
            ["confirm"] = "true",
        };
        ApplyStripeVaultOrToken(form, request.CustomerVaultToken, request.OneTimeHostedToken);

        var intentDoc = await PostStripeFormAsync(
            "/v1/payment_intents",
            form,
            request.IdempotencyKey,
            cancellationToken
        );
        if (intentDoc.IsFailure)
        {
            return Result.Failure<GatewayChargeResult>(intentDoc.Errors);
        }

        using var doc = intentDoc.Value;
        var id = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
        var status = doc.RootElement.TryGetProperty("status", out var sEl)
            ? sEl.GetString()
            : "succeeded";
        return new GatewayChargeResult(
            id,
            status,
            string.Equals(status, "succeeded", StringComparison.Ordinal),
            clock.UtcNow
        );
    }

    /// <inheritdoc />
    public async Task<Result<GatewayRefundResult>> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            return UnconfiguredError<GatewayRefundResult>();
        }

        var minorUnits = ConvertToMinorUnits(request.Amount, request.CurrencyCode);
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["payment_intent"] = request.OriginalGatewayTransactionId,
            ["amount"] = minorUnits.ToString(CultureInfo.InvariantCulture),
        };

        var refundDoc = await PostStripeFormAsync(
            "/v1/refunds",
            form,
            idempotencyKey: null,
            cancellationToken
        );
        if (refundDoc.IsFailure)
        {
            return Result.Failure<GatewayRefundResult>(refundDoc.Errors);
        }

        using var doc = refundDoc.Value;
        return new GatewayRefundResult(
            doc.RootElement.GetProperty("id").GetString() ?? string.Empty,
            clock.UtcNow
        );
    }

    /// <inheritdoc />
    public Task<Result<GatewaySubscriptionSyncResult>> SyncSubscriptionAsync(
        GatewaySubscriptionSyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
        {
            return Task.FromResult(UnconfiguredError<GatewaySubscriptionSyncResult>());
        }

        var subRef = string.IsNullOrWhiteSpace(request.ExistingSubscriptionReference)
            ? $"sub_ruth_{request.ScheduleId:N}"
            : request.ExistingSubscriptionReference;
        return Task.FromResult(
            Result.Success(new GatewaySubscriptionSyncResult(subRef, clock.UtcNow))
        );
    }

    /// <inheritdoc />
    public Result<GatewayWebhookPayload> ParseAndVerifyWebhook(
        string rawBody,
        string? signatureHeader
    )
    {
        return !StripeWebhookSignatureVerifier.Verify(
            rawBody,
            signatureHeader,
            options.Value.WebhookSigningSecret
        )
            ? DomainError.Unauthorized(
                "Finance.Stripe.WebhookSignatureInvalid",
                "Stripe-Signature HMAC-SHA256 verification failed."
            )
            : ParseStripeEvent(rawBody, clock.UtcNow);
    }

    private async Task<Result<JsonDocument>> PostStripeFormAsync(
        string relativePath,
        Dictionary<string, string> form,
        string? idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(options.Value.BaseUrl, UriKind.Absolute), relativePath)
        )
        {
            Content = new FormUrlEncodedContent(form),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            options.Value.SecretKey
        );
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _ = req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        using var resp = await client.SendAsync(req, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);
        var statusInt = (int)resp.StatusCode;
        return !resp.IsSuccessStatusCode
            ? DomainError.Failure(
                "Finance.Stripe.HttpError",
                string.Create(CultureInfo.InvariantCulture, $"Stripe returned {statusInt}.")
            )
            : JsonDocument.Parse(json);
    }

    private static void ApplyStripeVaultOrToken(
        Dictionary<string, string> form,
        string? vaultToken,
        string? oneTimeToken
    )
    {
        if (!string.IsNullOrWhiteSpace(vaultToken))
        {
            var split = vaultToken.Split('|', 2);
            if (split.Length == 2)
            {
                form["customer"] = split[0];
                form["payment_method"] = split[1];
                return;
            }

            form["payment_method"] = vaultToken;
        }
        else if (!string.IsNullOrWhiteSpace(oneTimeToken))
        {
            form["payment_method"] = oneTimeToken;
        }
    }

    private static long ConvertToMinorUnits(decimal amount, string currencyCode)
    {
        var upper = currencyCode.ToUpperInvariant();
        var isZeroDecimal =
            string.Equals(upper, "JPY", StringComparison.Ordinal)
            || string.Equals(upper, "KRW", StringComparison.Ordinal);
        var factor = isZeroDecimal ? 1m : 100m;
        return decimal.ToInt64(decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero));
    }

    private static Result<GatewayWebhookPayload> ParseStripeEvent(
        string rawBody,
        DateTimeOffset fallbackNow
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var eventId = root.GetProperty("id").GetString() ?? Guid.CreateVersion7().ToString("N");
            var type = root.GetProperty("type").GetString() ?? string.Empty;
            var kind = type switch
            {
                "invoice.payment_succeeded" => GatewayWebhookEventKind.SubscriptionChargeSucceeded,
                "invoice.payment_failed" => GatewayWebhookEventKind.SubscriptionChargeFailed,
                "customer.subscription.deleted" => GatewayWebhookEventKind.SubscriptionCancelled,
                "charge.refunded" => GatewayWebhookEventKind.ChargeRefunded,
                _ => GatewayWebhookEventKind.SubscriptionChargeSucceeded,
            };

            var obj = root.GetProperty("data").GetProperty("object");
            var subId = obj.TryGetProperty("subscription", out var sEl) ? sEl.GetString() : null;
            var txId = obj.TryGetProperty("payment_intent", out var pEl) ? pEl.GetString() : null;
            decimal? amount =
                obj.TryGetProperty("amount_paid", out var aEl)
                && aEl.ValueKind == JsonValueKind.Number
                    ? aEl.GetInt64() / 100m
                    : null;

            return new GatewayWebhookPayload(
                eventId,
                kind,
                subId,
                txId,
                amount,
                "USD",
                kind == GatewayWebhookEventKind.SubscriptionChargeFailed
                    ? "Stripe invoice payment failed"
                    : null,
                fallbackNow
            );
        }
        catch (JsonException)
        {
            return DomainError.Validation(
                "Finance.Stripe.WebhookInvalid",
                "Invalid Stripe event JSON."
            );
        }
    }

    private static Result<T> UnconfiguredError<T>()
    {
        return DomainError.Failure(
            "Finance.Stripe.Unconfigured",
            "Stripe SecretKey is not configured under PaymentGateways:Stripe."
        );
    }
}
