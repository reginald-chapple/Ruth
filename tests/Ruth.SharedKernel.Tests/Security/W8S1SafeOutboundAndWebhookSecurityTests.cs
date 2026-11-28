using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.SharedKernel.Tests.Security;

public sealed class W8S1SafeOutboundAndWebhookSecurityTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("0.0.0.0")]
    [InlineData("10.0.0.5")]
    [InlineData("100.64.0.1")]
    [InlineData("100.100.100.200")]
    [InlineData("169.254.169.254")]
    [InlineData("169.254.170.2")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.1.10")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.1.2.3")]
    [InlineData("fd00:ec2::254")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("64:ff9b::a9fe:a9fe")]
    [InlineData("2002:7f00:0001::1")]
    [InlineData("ff02::1")]
    public void OutboundAddressGuard_BlocksAllPrivateLoopbackMetadataAndMappedRanges(string rawIp)
    {
        var ip = IPAddress.Parse(rawIp);
        Assert.True(OutboundAddressGuard.IsDisallowedIpAddress(ip));
        var ex = Assert.Throws<OutboundSsrfBlockedException>(() =>
            OutboundAddressGuard.EnsureAllowedIpAddress(ip)
        );
        Assert.Equal("Outbound.PrivateOrReservedIpBlocked", ex.ReasonCode);
    }

    [Theory]
    [InlineData("93.184.216.34")]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946")]
    public void OutboundAddressGuard_PermitsPublicRoutableUnicastAddresses(string rawIp)
    {
        var ip = IPAddress.Parse(rawIp);
        Assert.False(OutboundAddressGuard.IsDisallowedIpAddress(ip));
        OutboundAddressGuard.EnsureAllowedIpAddress(ip);
    }

    [Theory]
    [InlineData("file:///etc/passwd", "Outbound.SchemeNotAllowed")]
    [InlineData("gopher://127.0.0.1:6379/_INFO", "Outbound.SchemeNotAllowed")]
    [InlineData("dict://127.0.0.1:6379/info", "Outbound.SchemeNotAllowed")]
    [InlineData("https://user:pass@hooks.example.org/webhook", "Outbound.UserInfoNotAllowed")]
    [InlineData("http://localhost:8080/admin", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://redis:6379/", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://postgres:5432/", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://rustfs:9000/", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://host.docker.internal:5000/", "Outbound.InternalHostnameBlocked")]
    [InlineData(
        "http://metadata.google.internal/computeMetadata/v1/",
        "Outbound.InternalHostnameBlocked"
    )]
    [InlineData("http://billing.svc.cluster.local/api", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://printer.local/zpl", "Outbound.InternalHostnameBlocked")]
    [InlineData("http://0177.0.0.1/admin", "Outbound.PrivateOrReservedIpBlocked")]
    [InlineData("http://2130706433/admin", "Outbound.PrivateOrReservedIpBlocked")]
    public void OutboundAddressGuard_BlocksDangerousSchemesUserInfoAndInternalHosts(
        string rawUri,
        string expectedReason
    )
    {
        var uri = new Uri(rawUri, UriKind.Absolute);
        var ex = Assert.Throws<OutboundSsrfBlockedException>(() =>
            OutboundAddressGuard.EnsureAllowedUri(uri)
        );
        Assert.Equal(expectedReason, ex.ReasonCode);
    }

    [Fact]
    public async Task SafeOutboundHttpHandler_RejectsDnsResolvedPrivateAndMetadataTargetsBeforePayloadTransmission()
    {
        var transmittedPayloads = 0;
        using var inner = new RecordingStubHandler(
            (_, _) =>
            {
                _ = Interlocked.Increment(ref transmittedPayloads);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        );

        using var safeHandler = new SafeOutboundHttpHandler(inner, dnsResolver: static (host, _) =>
            new ValueTask<IPAddress[]>(
                host.Contains("rebind", StringComparison.Ordinal)
                    ? [IPAddress.Parse("93.184.216.34"), IPAddress.Parse("169.254.169.254")]
                    : [IPAddress.Parse("10.24.0.9")]
            )
        );
        using var client = new HttpClient(safeHandler);

        var ex1 = await Assert.ThrowsAsync<OutboundSsrfBlockedException>(() =>
            client.PostAsync(
                new Uri("https://evil-dns.example.org/hook"),
                new StringContent("secret=never-sent")
            )
        );
        Assert.Equal("Outbound.PrivateOrReservedIpBlocked", ex1.ReasonCode);

        var ex2 = await Assert.ThrowsAsync<OutboundSsrfBlockedException>(() =>
            client.GetAsync(new Uri("https://rebind.example.org/feed"))
        );
        Assert.Equal("Outbound.PrivateOrReservedIpBlocked", ex2.ReasonCode);
        Assert.Equal(0, transmittedPayloads);
    }

    [Fact]
    public async Task SafeOutboundHttpHandler_BlocksCrossOriginAndMetadataRedirectHopsWhileAllowingSameOriginGet()
    {
        var calls = new List<Uri>();
        using var inner = new RecordingStubHandler(
            (req, _) =>
            {
                calls.Add(req.RequestUri!);
                if (
                    string.Equals(
                        req.RequestUri!.AbsolutePath,
                        "/hop-to-metadata",
                        StringComparison.Ordinal
                    )
                )
                {
                    var r = new HttpResponseMessage(HttpStatusCode.Found);
                    r.Headers.Location = new Uri("http://169.254.169.254/latest/meta-data/");
                    return r;
                }

                if (
                    string.Equals(
                        req.RequestUri.AbsolutePath,
                        "/hop-cross-origin",
                        StringComparison.Ordinal
                    )
                )
                {
                    var r = new HttpResponseMessage(HttpStatusCode.Found);
                    r.Headers.Location = new Uri("https://other-origin.example.net/target");
                    return r;
                }

                if (
                    string.Equals(
                        req.RequestUri.AbsolutePath,
                        "/same-origin-start",
                        StringComparison.Ordinal
                    )
                )
                {
                    var r = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
                    r.Headers.Location = new Uri("/same-origin-final", UriKind.Relative);
                    return r;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        );

        using var safeHandler = new SafeOutboundHttpHandler(inner, dnsResolver: static (_, _) =>
            new ValueTask<IPAddress[]>([IPAddress.Parse("93.184.216.34")])
        );
        using var client = new HttpClient(safeHandler);

        var metaHopEx = await Assert.ThrowsAsync<OutboundSsrfBlockedException>(() =>
            client.GetAsync(new Uri("https://partner.example.org/hop-to-metadata"))
        );
        Assert.Equal("Outbound.PrivateOrReservedIpBlocked", metaHopEx.ReasonCode);

        var crossOriginEx = await Assert.ThrowsAsync<OutboundSsrfBlockedException>(() =>
            client.GetAsync(new Uri("https://partner.example.org/hop-cross-origin"))
        );
        Assert.Equal("Outbound.CrossOriginRedirectBlocked", crossOriginEx.ReasonCode);

        using var sameOriginOk = await client.GetAsync(
            new Uri("https://partner.example.org/same-origin-start")
        );
        Assert.Equal(HttpStatusCode.OK, sameOriginOk.StatusCode);
        Assert.DoesNotContain(calls, static u =>
            string.Equals(u.Host, "169.254.169.254", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(calls, static u =>
            string.Equals(u.Host, "other-origin.example.net", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task SafeOutboundHttpHandler_SocketsConnectCallback_BlocksRealLoopbackSocketBeforeAccept()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var portText = ((IPEndPoint)listener.LocalEndpoint).Port.ToString(
            CultureInfo.InvariantCulture
        );

        using var socketsHandler = SafeOutboundHttpHandler.CreateSocketsHandler(
            dnsResolver: static (_, _) => new ValueTask<IPAddress[]>([IPAddress.Loopback])
        );
        using var client = new HttpClient(socketsHandler);

        _ = await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
            client.GetAsync(new Uri($"http://rebind-test.example.org:{portText}/internal"))
        );
        Assert.False(listener.Pending(), "No TCP connection may reach the loopback listener.");
    }

    [Fact]
    public void WebhookHmacReplayVerifier_AndDeduplicator_RejectUnsignedTamperedStaleAndReplayedPayloads()
    {
        var now = new DateTimeOffset(2026, 11, 27, 19, 0, 0, TimeSpan.Zero);
        const string secret = "whsec_unit_test_secret_key_2026";
        const string body = /*lang=json,strict*/
            """{"id":"evt_101","type":"account.updated"}""";

        var validHeader = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            body,
            secret,
            now
        );
        Assert.True(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(body, validHeader, secret, now)
        );

        // Unsigned / tampered body / tampered signature
        Assert.False(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                body,
                signatureHeader: null,
                secret,
                now
            )
        );
        Assert.False(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(body + " ", validHeader, secret, now)
        );
        var unixNow = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        Assert.False(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(
                body,
                $"t={unixNow},v1={new string('a', 64)}",
                secret,
                now
            )
        );

        // Stale past (> 5 min) & skewed future (> 5 min)
        var staleHeader = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            body,
            secret,
            now.AddMinutes(-6)
        );
        var futureHeader = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            body,
            secret,
            now.AddMinutes(6)
        );
        Assert.False(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(body, staleHeader, secret, now)
        );
        Assert.False(
            WebhookHmacReplayVerifier.VerifyTimestampedHeader(body, futureHeader, secret, now)
        );

        // Replay deduplicator within active window vs after expiry
        var deduplicator = new WebhookReplayDeduplicator();
        Assert.True(deduplicator.TryRegisterDelivery("stripe-connect", "evt_101", now));
        Assert.False(
            deduplicator.TryRegisterDelivery("stripe-connect", "evt_101", now.AddMinutes(2))
        );
        Assert.True(
            deduplicator.TryRegisterDelivery("stripe-connect", "evt_101", now.AddMinutes(6))
        );
    }

    private sealed class RecordingStubHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder
    ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
