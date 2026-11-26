namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 response listing serial numbers updated for a device registration.</summary>
/// <param name="SerialNumbers">Serial numbers updated since the client's previous tag.</param>
/// <param name="LastUpdated">Opaque monotonic tag the device passes back on its next passesUpdatedSince query.</param>
public sealed record PassKitSerialNumbersResponse(
    IReadOnlyList<string> SerialNumbers,
    string LastUpdated
);
