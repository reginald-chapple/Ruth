namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Dispatches Apple PassKit push-update notifications to registered devices when a pass changes.</summary>
public interface IWalletPassPushNotifier
{
    /// <summary>Notifies registered PassKit push tokens that a pass has been updated or revoked.</summary>
    /// <param name="passTypeIdentifier">PassKit passTypeIdentifier (APNs topic).</param>
    /// <param name="serialNumber">Updated pass serial number.</param>
    /// <param name="pushTokens">Distinct device push tokens registered for this pass.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of device push notifications accepted or recorded.</returns>
    Task<int> NotifyPassUpdatedAsync(
        string passTypeIdentifier,
        string serialNumber,
        IReadOnlyList<string> pushTokens,
        CancellationToken cancellationToken = default
    );
}
