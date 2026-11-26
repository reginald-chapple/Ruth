using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

internal sealed class LoopbackZplTcpCaptureServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly List<string> _payloads = [];
    private readonly Task _acceptTask;

    public LoopbackZplTcpCaptureServer(int expectedConnections)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Endpoint = string.Create(CultureInfo.InvariantCulture, $"127.0.0.1:{port}");
        _acceptTask = AcceptConnectionsAsync(expectedConnections);
    }

    public string Endpoint { get; }

    public async Task<IReadOnlyList<string>> WaitForPayloadsAsync(TimeSpan timeout)
    {
        await _acceptTask.WaitAsync(timeout);
        return _payloads;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private async Task AcceptConnectionsAsync(int expectedConnections)
    {
        for (var i = 0; i < expectedConnections; i++)
        {
            using var client = await _listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _payloads.Add(await reader.ReadToEndAsync());
        }
    }
}
