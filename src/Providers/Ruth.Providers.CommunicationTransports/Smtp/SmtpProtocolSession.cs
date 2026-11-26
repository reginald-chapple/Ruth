using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Ruth.SharedKernel.Results;

namespace Ruth.Providers.CommunicationTransports.Smtp;

/// <summary>Minimal RFC 5321 SMTP client session supporting EHLO, STARTTLS, AUTH PLAIN, and dot-stuffed DATA.</summary>
internal static class SmtpProtocolSession
{
    public static async Task<Result<string>> DeliverOverTcpAsync(
        SmtpTransportOptions options,
        string fromAddress,
        string toAddress,
        string mimePayload,
        Guid messageId,
        CancellationToken cancellationToken
    )
    {
        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Clamp(options.ConnectTimeoutMs, 500, 30_000));

        await client.ConnectAsync(options.Host!, options.Port, timeoutCts.Token);
        Stream stream = client.GetStream();
        if (options.UseImplicitTls)
        {
            stream = await UpgradeToTlsAsync(stream, options.Host!, timeoutCts.Token);
        }

        await using (stream)
        {
            return await ExecuteConversationAsync(
                stream,
                options,
                fromAddress,
                toAddress,
                mimePayload,
                messageId,
                timeoutCts.Token
            );
        }
    }

    private static async Task<Result<string>> ExecuteConversationAsync(
        Stream initialStream,
        SmtpTransportOptions options,
        string fromAddress,
        string toAddress,
        string mimePayload,
        Guid messageId,
        CancellationToken ct
    )
    {
        var activeStream = initialStream;
        var reader = CreateReader(activeStream);
        var writer = CreateWriter(activeStream);

        var banner = await ReadExpectedAsync(reader, 220, ct);
        if (banner.IsFailure)
        {
            return Result.Failure<string>(banner.Errors);
        }

        var ehlo = await SendCommandAsync(reader, writer, "EHLO ruth.local", 250, ct);
        if (ehlo.IsFailure)
        {
            return Result.Failure<string>(ehlo.Errors);
        }

        if (options.UseStartTls && !options.UseImplicitTls)
        {
            var startTls = await SendCommandAsync(reader, writer, "STARTTLS", 220, ct);
            if (startTls.IsFailure)
            {
                return Result.Failure<string>(startTls.Errors);
            }

            activeStream = await UpgradeToTlsAsync(activeStream, options.Host!, ct);
            reader = CreateReader(activeStream);
            writer = CreateWriter(activeStream);
            _ = await SendCommandAsync(reader, writer, "EHLO ruth.local", 250, ct);
        }

        var authResult = await AuthenticateIfConfiguredAsync(reader, writer, options, ct);
        return authResult.IsFailure
            ? Result.Failure<string>(authResult.Errors)
            : await SendEnvelopeAndBodyAsync(
                reader,
                writer,
                fromAddress,
                toAddress,
                mimePayload,
                messageId,
                ct
            );
    }

    private static async Task<Result> AuthenticateIfConfiguredAsync(
        StreamReader reader,
        StreamWriter writer,
        SmtpTransportOptions options,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return Result.Success();
        }

        var rawAuth = $"\0{options.Username}\0{options.Password ?? string.Empty}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawAuth));
        return await SendCommandAsync(reader, writer, $"AUTH PLAIN {encoded}", 235, ct);
    }

    private static async Task<Result<string>> SendEnvelopeAndBodyAsync(
        StreamReader reader,
        StreamWriter writer,
        string fromAddress,
        string toAddress,
        string mimePayload,
        Guid messageId,
        CancellationToken ct
    )
    {
        var mailFrom = await SendCommandAsync(
            reader,
            writer,
            $"MAIL FROM:<{SmtpMimeBuilder.SanitizeHeader(fromAddress)}>",
            250,
            ct
        );
        if (mailFrom.IsFailure)
        {
            return Result.Failure<string>(mailFrom.Errors);
        }

        var rcptTo = await SendCommandAsync(
            reader,
            writer,
            $"RCPT TO:<{SmtpMimeBuilder.SanitizeHeader(toAddress)}>",
            250,
            ct
        );
        if (rcptTo.IsFailure)
        {
            return Result.Failure<string>(rcptTo.Errors);
        }

        var dataReady = await SendCommandAsync(reader, writer, "DATA", 354, ct);
        if (dataReady.IsFailure)
        {
            return Result.Failure<string>(dataReady.Errors);
        }

        var dotStuffed = mimePayload.Replace("\r\n.", "\r\n..", StringComparison.Ordinal);
        await writer.WriteAsync(dotStuffed.AsMemory(), ct);
        if (!dotStuffed.EndsWith("\r\n", StringComparison.Ordinal))
        {
            await writer.WriteAsync("\r\n".AsMemory(), ct);
        }

        var accepted = await SendCommandAsync(reader, writer, ".", 250, ct);
        if (accepted.IsFailure)
        {
            return Result.Failure<string>(accepted.Errors);
        }

        _ = await SendCommandAsync(reader, writer, "QUIT", 221, ct);
        return $"smtp-{messageId:N}";
    }

    private static async Task<SslStream> UpgradeToTlsAsync(
        Stream innerStream,
        string targetHost,
        CancellationToken ct
    )
    {
        var ssl = new SslStream(innerStream, leaveInnerStreamOpen: false);
        await ssl.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions { TargetHost = targetHost },
            ct
        );
        return ssl;
    }

    private static StreamReader CreateReader(Stream stream)
    {
        return new StreamReader(
            stream,
            Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true
        );
    }

    private static StreamWriter CreateWriter(Stream stream)
    {
        return new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true,
        };
    }

    private static async Task<Result> SendCommandAsync(
        StreamReader reader,
        StreamWriter writer,
        string command,
        int expectedCode,
        CancellationToken ct
    )
    {
        await writer.WriteLineAsync(command.AsMemory(), ct);
        return await ReadExpectedAsync(reader, expectedCode, ct);
    }

    private static async Task<Result> ReadExpectedAsync(
        StreamReader reader,
        int expectedCode,
        CancellationToken ct
    )
    {
        string? line;
        while (true)
        {
            line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                return DomainError.Failure(
                    "Smtp.UnexpectedEof",
                    "SMTP server closed connection unexpectedly."
                );
            }

            if (line.Length < 4 || line[3] != '-')
            {
                break;
            }
        }

        return
            line.Length >= 3
            && int.TryParse(line.AsSpan(0, 3), CultureInfo.InvariantCulture, out var code)
            && code == expectedCode
            ? Result.Success()
            : DomainError.Failure(
                "Smtp.UnexpectedReply",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"SMTP reply '{line}' did not match {expectedCode}."
                )
            );
    }
}
