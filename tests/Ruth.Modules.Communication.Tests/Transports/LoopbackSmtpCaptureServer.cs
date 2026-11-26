using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ruth.Modules.Communication.Tests.Transports;

internal sealed class LoopbackSmtpCaptureServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _serverTask;
    private readonly StringBuilder _transcript = new();
    private readonly StringBuilder _capturedData = new();

    public LoopbackSmtpCaptureServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _serverTask = RunSingleSessionAsync();
    }

    public int Port { get; }
    public string Transcript => _transcript.ToString();
    public string CapturedData => _capturedData.ToString();

    public Task WaitForCompletionAsync(TimeSpan timeout)
    {
        return _serverTask.WaitAsync(timeout);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private async Task RunSingleSessionAsync()
    {
        using var client = await _listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII)
        {
            NewLine = "\r\n",
            AutoFlush = true,
        };

        await writer.WriteLineAsync("220 localhost ESMTP RuthMock");
        var inData = false;
        while (await reader.ReadLineAsync() is { } line)
        {
            _ = _transcript.AppendLine(line);
            if (inData)
            {
                if (string.Equals(line, ".", StringComparison.Ordinal))
                {
                    inData = false;
                    await writer.WriteLineAsync("250 2.0.0 Queued");
                }
                else
                {
                    _ = _capturedData.AppendLine(line);
                }

                continue;
            }

            if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250-localhost");
                await writer.WriteLineAsync("250 AUTH PLAIN");
            }
            else if (line.StartsWith("AUTH PLAIN", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("235 2.7.0 Authentication successful");
            }
            else if (
                line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase)
            )
            {
                await writer.WriteLineAsync("250 2.1.0 OK");
            }
            else if (string.Equals(line, "DATA", StringComparison.OrdinalIgnoreCase))
            {
                inData = true;
                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 2.0.0 Bye");
                break;
            }
        }
    }
}
