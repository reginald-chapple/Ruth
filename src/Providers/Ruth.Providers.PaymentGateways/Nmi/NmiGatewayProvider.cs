using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.PaymentGateways.Nmi;

/// <summary>NMI Direct Post, Customer Vault, and Recurring Billing payment gateway adapter.</summary>
public sealed class NmiGatewayProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<NmiGatewayOptions> options,
    IClock clock
) : IPaymentGateway
{
    /// <summary>HttpClient logical name.</summary>
    public const string HttpClientName = "Ruth.PaymentGateways.Nmi";

    /// <inheritdoc />
    public string ProviderKey => "nmi";

    /// <inheritdoc />
    public string DisplayName => "NMI (Network Merchants Customer Vault & Direct Post)";

    /// <inheritdoc />
    public async Task<Result<GatewayCustomerVaultResult>> VaultPaymentMethodAsync(
        GatewayVaultRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var securityKey = options.Value.SecurityKey;
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            return UnconfiguredError<GatewayCustomerVaultResult>();
        }

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["security_key"] = securityKey,
            ["customer_vault"] = "add_customer",
            ["payment_token"] = request.HostedPaymentToken,
            ["payment"] =
                request.MethodKind == PaymentMethodKind.AchBankAccount ? "check" : "creditcard",
        };
        if (!string.IsNullOrWhiteSpace(request.BillingEmail))
        {
            form["email"] = request.BillingEmail;
        }

        var response = await PostDirectAsync(form, cancellationToken);
        if (response.IsFailure)
        {
            return Result.Failure<GatewayCustomerVaultResult>(response.Errors);
        }

        var vaultId = response.Value.GetValueOrDefault("customer_vault_id");
        if (string.IsNullOrWhiteSpace(vaultId))
        {
            return DomainError.Failure(
                "Finance.Nmi.MissingVaultId",
                "NMI did not return customer_vault_id."
            );
        }

        var last4 = ExtractDigitsTail(request.MaskedLast4Hint, 4, "0000");
        var brand = request.CardBrandHint
            ?? (request.MethodKind == PaymentMethodKind.AchBankAccount ? "ACH" : "Card");
        return new GatewayCustomerVaultResult(
            vaultId,
            $"****{last4}",
            brand,
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
        var securityKey = options.Value.SecurityKey;
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            return UnconfiguredError<GatewayChargeResult>();
        }

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["security_key"] = securityKey,
            ["type"] = "sale",
            ["amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
            ["currency"] = request.CurrencyCode,
            ["orderid"] = request.IdempotencyKey,
        };
        if (!string.IsNullOrWhiteSpace(request.CustomerVaultToken))
        {
            form["customer_vault_id"] = request.CustomerVaultToken;
        }
        else if (!string.IsNullOrWhiteSpace(request.OneTimeHostedToken))
        {
            form["payment_token"] = request.OneTimeHostedToken;
        }

        var response = await PostDirectAsync(form, cancellationToken);
        if (response.IsFailure)
        {
            return Result.Failure<GatewayChargeResult>(response.Errors);
        }

        var txId = response.Value.GetValueOrDefault("transactionid") ?? string.Empty;
        var authCode = response.Value.GetValueOrDefault("authcode");
        return new GatewayChargeResult(
            txId,
            authCode,
            request.MethodKind == PaymentMethodKind.CreditCard,
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
        var securityKey = options.Value.SecurityKey;
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            return UnconfiguredError<GatewayRefundResult>();
        }

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["security_key"] = securityKey,
            ["type"] = "refund",
            ["transactionid"] = request.OriginalGatewayTransactionId,
            ["amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
        };

        var response = await PostDirectAsync(form, cancellationToken);
        return response.IsFailure
            ? Result.Failure<GatewayRefundResult>(response.Errors)
            : new GatewayRefundResult(
                response.Value.GetValueOrDefault("transactionid")
                    ?? request.OriginalGatewayTransactionId,
                clock.UtcNow
            );
    }

    /// <inheritdoc />
    public async Task<Result<GatewaySubscriptionSyncResult>> SyncSubscriptionAsync(
        GatewaySubscriptionSyncRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var securityKey = options.Value.SecurityKey;
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            return UnconfiguredError<GatewaySubscriptionSyncResult>();
        }

        var form = BuildSubscriptionForm(request, securityKey);
        var response = await PostDirectAsync(form, cancellationToken);
        if (response.IsFailure)
        {
            return Result.Failure<GatewaySubscriptionSyncResult>(response.Errors);
        }

        var subId = response.Value.GetValueOrDefault("subscription_id")
            ?? request.ExistingSubscriptionReference
            ?? $"nmi_sub_{request.ScheduleId:N}";
        return new GatewaySubscriptionSyncResult(subId, clock.UtcNow);
    }

    /// <inheritdoc />
    public Result<GatewayWebhookPayload> ParseAndVerifyWebhook(
        string rawBody,
        string? signatureHeader
    )
    {
        return !NmiWebhookSignatureVerifier.Verify(
            rawBody,
            signatureHeader,
            options.Value.WebhookSigningKey
        )
            ? DomainError.Unauthorized(
                "Finance.Nmi.WebhookSignatureInvalid",
                "NMI Webhook-Signature HMAC verification failed."
            )
            : ParseWebhookJson(rawBody, clock.UtcNow);
    }

    private async Task<Result<Dictionary<string, string>>> PostDirectAsync(
        Dictionary<string, string> form,
        CancellationToken cancellationToken
    )
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var content = new FormUrlEncodedContent(form);
        using var httpResponse = await client.PostAsync(
            new Uri(options.Value.EndpointUrl, UriKind.Absolute),
            content,
            cancellationToken
        );
        var raw = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var parsed = ParseQueryStringResponse(raw);
        if (!string.Equals(parsed.GetValueOrDefault("response"), "1", StringComparison.Ordinal))
        {
            var reason = parsed.GetValueOrDefault("responsetext")
                ?? "NMI gateway rejected transaction.";
            return DomainError.Failure("Finance.Nmi.Declined", reason);
        }

        return parsed;
    }

    private static Dictionary<string, string> BuildSubscriptionForm(
        GatewaySubscriptionSyncRequest request,
        string securityKey
    )
    {
        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["security_key"] = securityKey,
        };

        if (
            request.Action == GatewaySubscriptionAction.Cancel
            && !string.IsNullOrWhiteSpace(request.ExistingSubscriptionReference)
        )
        {
            form["recurring"] = "delete_subscription";
            form["subscription_id"] = request.ExistingSubscriptionReference;
            return form;
        }

        form["recurring"] = string.IsNullOrWhiteSpace(request.ExistingSubscriptionReference)
            ? "add_subscription"
            : "update_subscription";
        if (!string.IsNullOrWhiteSpace(request.ExistingSubscriptionReference))
        {
            form["subscription_id"] = request.ExistingSubscriptionReference;
        }

        form["customer_vault_id"] = request.CustomerVaultToken;
        form["plan_payments"] = "0";
        form["plan_amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
        form["start_date"] = request.NextPaymentDate.ToString(
            "yyyyMMdd",
            CultureInfo.InvariantCulture
        );
        form["month_frequency"] =
            request.Frequency == PledgeFrequency.Quarterly ? "3"
            : request.Frequency == PledgeFrequency.Annually ? "12"
            : "1";
        form["day_of_month"] = Math.Min(request.NextPaymentDate.Day, 28)
            .ToString(CultureInfo.InvariantCulture);
        return form;
    }

    private static Dictionary<string, string> ParseQueryStringResponse(string raw)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                dict[Uri.UnescapeDataString(pair[..eq])] = Uri.UnescapeDataString(pair[(eq + 1)..]);
            }
        }

        return dict;
    }

    private static Result<GatewayWebhookPayload> ParseWebhookJson(
        string rawBody,
        DateTimeOffset fallbackNow
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var eventId = root.GetProperty("event_id").GetString()
                ?? Guid.CreateVersion7().ToString("N");
            var eventType = root.GetProperty("event_type").GetString() ?? string.Empty;
            var kind = eventType switch
            {
                "transaction.sale.success" => GatewayWebhookEventKind.SubscriptionChargeSucceeded,
                "transaction.sale.failure" => GatewayWebhookEventKind.SubscriptionChargeFailed,
                "recurring.subscription.deleted" => GatewayWebhookEventKind.SubscriptionCancelled,
                "transaction.refund.success" => GatewayWebhookEventKind.ChargeRefunded,
                _ => GatewayWebhookEventKind.SubscriptionChargeSucceeded,
            };

            var body = root.TryGetProperty("event_body", out var bodyEl) ? bodyEl : root;
            var subId = body.TryGetProperty("subscription_id", out var sEl)
                ? sEl.GetString()
                : null;
            var txId = body.TryGetProperty("transaction_id", out var tEl) ? tEl.GetString() : null;
            decimal? amount =
                body.TryGetProperty("amount", out var aEl)
                && decimal.TryParse(
                    aEl.ToString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var parsedAmt
                )
                    ? parsedAmt
                    : null;

            return new GatewayWebhookPayload(
                eventId,
                kind,
                subId,
                txId,
                amount,
                "USD",
                FailureMessage: kind == GatewayWebhookEventKind.SubscriptionChargeFailed
                    ? "NMI sale failure"
                    : null,
                fallbackNow
            );
        }
        catch (JsonException)
        {
            return DomainError.Validation(
                "Finance.Nmi.WebhookInvalid",
                "Invalid NMI webhook JSON."
            );
        }
    }

    private static string ExtractDigitsTail(string? raw, int length, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var digits = new string([.. raw.Where(char.IsAsciiDigit)]);
        return digits.Length >= length ? digits[^length..] : fallback;
    }

    private static Result<T> UnconfiguredError<T>()
    {
        return DomainError.Failure(
            "Finance.Nmi.Unconfigured",
            "NMI SecurityKey is not configured under PaymentGateways:Nmi."
        );
    }
}
