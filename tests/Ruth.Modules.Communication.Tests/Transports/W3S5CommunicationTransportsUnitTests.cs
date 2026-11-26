using System.Net;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.Providers.CommunicationTransports.Mailgun;
using Ruth.Providers.CommunicationTransports.SendGrid;
using Ruth.Providers.CommunicationTransports.Smtp;
using Ruth.Providers.CommunicationTransports.Twilio;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Transports;

public sealed class W3S5CommunicationTransportsUnitTests
{
    private static readonly IClock _clock = new SystemClock();

    [Fact]
    public async Task SmtpCommunicationTransport_DeliversAuthenticatedMultipartMimeOverTcp()
    {
        using var smtpServer = new LoopbackSmtpCaptureServer();
        var options = Options.Create(
            new SmtpTransportOptions
            {
                Host = "127.0.0.1",
                Port = smtpServer.Port,
                Username = "relay-user",
                Password = "relay-secret",
                DefaultFromEmail = "noreply@ruth.org",
                DefaultFromName = "Ruth Org",
            }
        );

        var transport = new SmtpCommunicationTransport(options, _clock);
        Assert.True(transport.IsConfigured);

        var msg = new OutboundTransportMessage(
            Guid.CreateVersion7(),
            CommunicationChannel.Email,
            "member@example.org",
            ToDisplayName: "Alex Rivera",
            Subject: "Receipt #104",
            HtmlBody: "<p>Thank you!</p>",
            PlainTextBody: "Thank you!"
        );

        var sent = await transport.SendAsync(msg);
        await smtpServer.WaitForCompletionAsync(TimeSpan.FromSeconds(5));

        Assert.True(sent.IsSuccess);
        Assert.Equal("smtp", sent.Value.TransportKey);
        Assert.False(sent.Value.IsSimulated);
        Assert.Contains("AUTH PLAIN", smtpServer.Transcript, StringComparison.Ordinal);
        Assert.Contains(
            "MAIL FROM:<noreply@ruth.org>",
            smtpServer.Transcript,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "RCPT TO:<member@example.org>",
            smtpServer.Transcript,
            StringComparison.Ordinal
        );
        Assert.Contains("multipart/alternative", smtpServer.CapturedData, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendGrid_Mailgun_And_TwilioSms_Transports_SendAuthenticatedRequests()
    {
        // 1. SendGrid v3
        using var sgHandler = new RecordingHttpMessageHandler(
            HttpStatusCode.Accepted,
            string.Empty,
            new KeyValuePair<string, string>("X-Message-Id", "sg-msg-9001")
        );
        var sg = new SendGridCommunicationTransport(
            new SingleHttpClientFactory(sgHandler),
            Options.Create(new SendGridTransportOptions { ApiKey = "SG.test_key" }),
            _clock
        );
        var sgResult = await sg.SendAsync(
            new OutboundTransportMessage(
                Guid.CreateVersion7(),
                CommunicationChannel.Email,
                "donor@example.org",
                Subject: "Hello",
                HtmlBody: "<p>Hi</p>"
            )
        );
        Assert.True(sgResult.IsSuccess);
        Assert.Equal("sg-msg-9001", sgResult.Value.ProviderMessageId);
        Assert.Equal("Bearer", sgHandler.LastAuthScheme);

        // 2. Mailgun v3
        using var mgHandler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            /*lang=json,strict*/
            """{"id":"<20261125.mg@mail.ruth.org>","message":"Queued."}"""
        );
        var mg = new MailgunCommunicationTransport(
            new SingleHttpClientFactory(mgHandler),
            Options.Create(
                new MailgunTransportOptions { ApiKey = "key-123", Domain = "mg.ruth.org" }
            ),
            _clock
        );
        var mgResult = await mg.SendAsync(
            new OutboundTransportMessage(
                Guid.CreateVersion7(),
                CommunicationChannel.Email,
                "staff@example.org",
                Subject: "Alert",
                PlainTextBody: "Alert body"
            )
        );
        Assert.True(mgResult.IsSuccess);
        Assert.Equal("<20261125.mg@mail.ruth.org>", mgResult.Value.ProviderMessageId);
        Assert.Equal("Basic", mgHandler.LastAuthScheme);
        Assert.Contains(
            "/v3/mg.ruth.org/messages",
            mgHandler.LastRequestUri!.AbsolutePath,
            StringComparison.Ordinal
        );

        // 3. Twilio Programmable SMS
        using var twHandler = new RecordingHttpMessageHandler(
            HttpStatusCode.Created,
            /*lang=json,strict*/
            """{"sid":"SM9988776655","status":"queued"}"""
        );
        var tw = new TwilioSmsCommunicationTransport(
            new SingleHttpClientFactory(twHandler),
            Options.Create(
                new TwilioSmsTransportOptions
                {
                    AccountSid = "AC12345",
                    AuthToken = "secret",
                    DefaultFromNumber = "+15550001111",
                }
            ),
            _clock
        );
        var twResult = await tw.SendAsync(
            new OutboundTransportMessage(
                Guid.CreateVersion7(),
                CommunicationChannel.Sms,
                "+15552223333",
                PlainTextBody: "Shift reminder"
            )
        );
        Assert.True(twResult.IsSuccess);
        Assert.Equal("SM9988776655", twResult.Value.ProviderMessageId);
        Assert.Contains("To=%2B15552223333", twHandler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevInMemorySink_PushStub_And_ResolverFallback_WorkWhenExternalProvidersUnconfigured()
    {
        var sink = new InMemoryCommunicationSink();
        var devEmail = new DevInMemoryEmailTransport(sink, _clock);
        var devSms = new DevInMemorySmsTransport(sink, _clock);
        var pushStub = new PushNotificationStubTransport(sink, _clock);
        var unconfiguredSmtp = new SmtpCommunicationTransport(
            Options.Create(new SmtpTransportOptions()),
            _clock
        );

        var resolver = new CommunicationTransportResolver(
            [unconfiguredSmtp, devEmail, devSms, pushStub],
            Options.Create(new CommunicationTransportOptions { PreferredEmailTransport = "smtp" })
        );

        var activeEmail = resolver.ResolveForChannel(CommunicationChannel.Email);
        Assert.Equal(DevInMemoryEmailTransport.Key, activeEmail.TransportKey);

        var pushRes = await resolver
            .ResolveForChannel(CommunicationChannel.Push)
            .SendAsync(
                new OutboundTransportMessage(
                    Guid.CreateVersion7(),
                    CommunicationChannel.Push,
                    "apns-token-xyz123456",
                    PlainTextBody: "Room check-in alert",
                    PushTitle: "Safety Alert"
                )
            );
        Assert.True(pushRes.IsSuccess);
        Assert.True(pushRes.Value.IsSimulated);
        var captured = Assert.Single(sink.Query(CommunicationChannel.Push));
        Assert.Equal("Safety Alert", captured.PushTitle);
    }
}
