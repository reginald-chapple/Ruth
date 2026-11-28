using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Ruth.Providers.PaymentGateways.Nmi;
using Ruth.Providers.PaymentGateways.Stripe;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Tests;

internal static class GatewayProviderTestHttpFactory
{
    public static NmiGatewayProvider CreateNmiProvider()
    {
        var factory = new StubHttpClientFactory(static _ => new HttpResponseMessage(
            HttpStatusCode.OK
        )
        {
            Content = new StringContent(
                "response=1&responsetext=SUCCESS&customer_vault_id=nvault_1&transactionid=ntx_1&authcode=A1&subscription_id=nsub_1"
            ),
        });
        var opts = Options.Create(
            new NmiGatewayOptions
            {
                SecurityKey = "nmi-key",
                WebhookSigningKey = "nmi-secret",
                EndpointUrl = "https://nmi.ruth.test/api/transact.php",
            }
        );
        return new NmiGatewayProvider(factory, opts, new SystemClock());
    }

    public static StripePaymentGatewayProvider CreateStripeProvider()
    {
        var factory = new StubHttpClientFactory(static req =>
        {
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;
            object payload =
                path.Contains("customers", StringComparison.Ordinal) ? new { id = "cus_123" }
                : path.Contains("refunds", StringComparison.Ordinal) ? new { id = "re_123" }
                : new { id = "pi_123", status = "succeeded" };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(payload),
            };
        });
        var opts = Options.Create(
            new StripeGatewayOptions
            {
                SecretKey = "sk_test_123",
                WebhookSigningSecret = "whsec_test",
                BaseUrl = "https://stripe.ruth.test",
            }
        );
        return new StripePaymentGatewayProvider(factory, opts, new SystemClock());
    }

    private sealed class StubHttpClientFactory(
        Func<HttpRequestMessage, HttpResponseMessage> responder
    ) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHandler(responder), disposeHandler: true);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(responder(request));
        }
    }
}
