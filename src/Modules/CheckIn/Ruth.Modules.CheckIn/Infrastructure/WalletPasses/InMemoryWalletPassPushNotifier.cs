using System.Collections.Concurrent;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;

namespace Ruth.Modules.CheckIn.Infrastructure.WalletPasses;

/// <summary>Default in-memory PassKit push notifier capturing dispatched push tokens for diagnostics and deterministic tests.</summary>
public sealed class InMemoryWalletPassPushNotifier : IWalletPassPushNotifier
{
    private readonly ConcurrentQueue<(
        string PassTypeIdentifier,
        string SerialNumber,
        string PushToken
    )> _dispatched = new();

    /// <summary>Gets a snapshot of all PassKit push notifications dispatched in this process.</summary>
    public IReadOnlyList<(
        string PassTypeIdentifier,
        string SerialNumber,
        string PushToken
    )> GetDispatchedNotifications()
    {
        return [.. _dispatched];
    }

    /// <inheritdoc />
    public Task<int> NotifyPassUpdatedAsync(
        string passTypeIdentifier,
        string serialNumber,
        IReadOnlyList<string> pushTokens,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(pushTokens);
        var count = 0;
        foreach (
            var token in pushTokens
                .Where(static t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal)
        )
        {
            _dispatched.Enqueue((passTypeIdentifier, serialNumber, token));
            count++;
        }

        return Task.FromResult(count);
    }
}
