using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Ruth.E2E.Tests.Tier4;

internal sealed class TcpForwardingProxy : IAsyncDisposable
{
    private readonly string _targetHost;
    private readonly int _targetPort;
    private readonly ConcurrentDictionary<int, TcpClient> _activeClients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _sessionCts;
    private Task? _acceptLoopTask;
    private int _nextClientId;

    public TcpForwardingProxy(Uri targetEndpoint)
    {
        ArgumentNullException.ThrowIfNull(targetEndpoint);
        _targetHost = targetEndpoint.Host;
        _targetPort = targetEndpoint.Port;
        Port = AllocatePort();
    }

    public int Port { get; }

    public string EndpointUrl =>
        string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{Port}");

    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        _sessionCts = new CancellationTokenSource();
        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Server.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            optionValue: true
        );
        listener.Start();

        _listener = listener;
        _acceptLoopTask = Task.Run(
            () => AcceptLoopAsync(listener, _sessionCts.Token),
            _sessionCts.Token
        );
    }

    public async Task StopAsync()
    {
        var listener = _listener;
        _listener = null;

        if (_sessionCts is not null)
        {
            await _sessionCts.CancelAsync();
        }

        listener?.Stop();

        foreach (var entry in _activeClients)
        {
            if (_activeClients.TryRemove(entry.Key, out var client))
            {
                client.Dispose();
            }
        }

        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Proxy shutdown cancelled accept loop.
            }

            _acceptLoopTask = null;
        }

        _sessionCts?.Dispose();
        _sessionCts = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => RelayClientAsync(client, ct), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    private async Task RelayClientAsync(TcpClient inbound, CancellationToken ct)
    {
        var inboundId = Interlocked.Increment(ref _nextClientId);
        _activeClients[inboundId] = inbound;

        using var outbound = new TcpClient();
        try
        {
            await outbound.ConnectAsync(_targetHost, _targetPort, ct);
            await using var inStream = inbound.GetStream();
            await using var outStream = outbound.GetStream();

            var upstream = inStream.CopyToAsync(outStream, ct);
            var downstream = outStream.CopyToAsync(inStream, ct);
            _ = await Task.WhenAny(upstream, downstream);
        }
        catch (OperationCanceledException)
        {
            // Proxy stopped for outage simulation.
        }
        catch (SocketException)
        {
            // Upstream or downstream closed.
        }
        catch (IOException)
        {
            // Stream closed during relay.
        }
        finally
        {
            _ = _activeClients.TryRemove(inboundId, out _);
            inbound.Dispose();
        }
    }

    private static int AllocatePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }
}
