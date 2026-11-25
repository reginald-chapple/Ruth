using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Providers.Checkr;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class CheckrProviderDomainTests
{
    [Theory]
    [InlineData("invitation.created", null, BackgroundCheckStatus.InvitationSent)]
    [InlineData("invitation.completed", null, BackgroundCheckStatus.InvitationCompleted)]
    [InlineData("invitation.expired", null, BackgroundCheckStatus.InvitationExpired)]
    [InlineData("report.created", null, BackgroundCheckStatus.ReportPending)]
    [InlineData("report.upgraded", null, BackgroundCheckStatus.ReportPending)]
    [InlineData("report.resumed", null, BackgroundCheckStatus.ReportPending)]
    [InlineData("report.suspended", null, BackgroundCheckStatus.ReportSuspended)]
    [InlineData("report.disputed", null, BackgroundCheckStatus.ReportSuspended)]
    [InlineData("report.updated", "clear", BackgroundCheckStatus.ReportClear)]
    [InlineData("report.updated", "consider", BackgroundCheckStatus.ReportConsider)]
    [InlineData("report.completed", "suspended", BackgroundCheckStatus.ReportSuspended)]
    [InlineData("report.completed", "dispute", BackgroundCheckStatus.ReportSuspended)]
    [InlineData("report.completed", "pending", BackgroundCheckStatus.ReportPending)]
    [InlineData("candidate.created", null, BackgroundCheckStatus.ReportPending)]
    public void CheckrWebhookParser_MapsAllSupportedAndFallbackEvents(
        string eventType,
        string? status,
        BackgroundCheckStatus expected
    )
    {
        var statusProp = status is null ? string.Empty : $",\"status\":\"{status}\"";
        var payload =
            "{\"id\":\"evt_1\",\"type\":\""
            + eventType
            + "\",\"data\":{\"object\":{\"id\":\"obj_1\",\"candidate_id\":\"cand_1\""
            + statusProp
            + "}}}";

        var result = CheckrWebhookParser.Parse(payload);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.MappedStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    [InlineData( /*lang=json,strict*/
        """{"type":"report.completed"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"report.completed","data":{"object":{"id":"obj_1"}}}"""
    )]
    public void CheckrWebhookParser_RejectsMalformedPayloads(string payload)
    {
        Assert.True(CheckrWebhookParser.Parse(payload).IsFailure);
    }

    [Fact]
    public async Task CheckrBackgroundCheckProvider_UnconfiguredFallback_Works()
    {
        using var handler = new StubHttpMessageHandler(
            (_, _) => throw new InvalidOperationException()
        );
        using var client = new HttpClient(handler);
        var unconfigured = new CheckrBackgroundCheckProvider(
            client,
            Options.Create(new CheckrOptions())
        );
        var defaultPkgs = await unconfigured.GetPackagesAsync();
        Assert.True(defaultPkgs.IsSuccess);
        _ = Assert.Single(defaultPkgs.Value);

        var submitRequest = new BackgroundCheckSubmissionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Alex",
            "J",
            "Rivera",
            "alex@ruth.test",
            "tasker_standard"
        );
        Assert.True((await unconfigured.SubmitRequestAsync(submitRequest)).IsFailure);
    }

    [Fact]
    public async Task CheckrBackgroundCheckProvider_ConfiguredFlow_Works()
    {
        using var handler = new StubHttpMessageHandler(BuildCheckrSuccessResponse);
        using var client = new HttpClient(handler);
        var configured = new CheckrBackgroundCheckProvider(
            client,
            Options.Create(new CheckrOptions { ApiKey = "test_key", WebhookSecret = "sec_1" })
        );
        var pkgs = await configured.GetPackagesAsync();
        Assert.True(pkgs.IsSuccess);
        _ = Assert.Single(pkgs.Value);

        var submitRequest = new BackgroundCheckSubmissionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Alex",
            "J",
            "Rivera",
            "alex@ruth.test",
            "tasker_standard"
        );
        var submitted = await configured.SubmitRequestAsync(submitRequest);
        Assert.True(submitted.IsSuccess);
        Assert.Equal("cand_42", submitted.Value.ProviderRequestId);
        Assert.Equal("https://checkr.test/invite/42", submitted.Value.InvitationUrl);
        Assert.True(
            configured.VerifyWebhookSignature(
                "body",
                CheckrWebhookSignatureVerifier.ComputeSignature("body", "sec_1")
            )
        );
        Assert.True(configured.ParseWebhookEvent("{}").IsFailure);
    }

    [Fact]
    public async Task CheckrBackgroundCheckProvider_HandlesHttpErrorResponses()
    {
        using var errorHandler = new StubHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.BadGateway)
        );
        using var client = new HttpClient(errorHandler);
        var provider = new CheckrBackgroundCheckProvider(
            client,
            Options.Create(new CheckrOptions { ApiKey = "test_key" })
        );

        Assert.True((await provider.GetPackagesAsync()).IsFailure);
        var req = new BackgroundCheckSubmissionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Sam",
            MiddleName: null,
            "Lee",
            "sam@ruth.test",
            "tasker_standard"
        );
        Assert.True((await provider.SubmitRequestAsync(req)).IsFailure);
    }

    private static HttpResponseMessage BuildCheckrSuccessResponse(
        HttpRequestMessage req,
        CancellationToken _
    )
    {
        var path = req.RequestUri?.AbsolutePath ?? string.Empty;
        var isPackages = path.EndsWith("v1/packages", StringComparison.Ordinal);
        var isCandidates = path.EndsWith("v1/candidates", StringComparison.Ordinal);

        var (code, body) =
            isPackages
                ? (
                    HttpStatusCode.OK,
                    /*lang=json,strict*/
                    """{"data":[{"slug":"tasker_standard","name":"Standard"},{"slug":""}]}"""
                )
            : isCandidates
                ? (
                    HttpStatusCode.Created, /*lang=json,strict*/
                    """{"id":"cand_42"}"""
                )
            : (
                HttpStatusCode.Created,
                /*lang=json,strict*/
                """{"invitation_url":"https://checkr.test/invite/42"}"""
            );

        return new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHttpMessageHandler(
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
