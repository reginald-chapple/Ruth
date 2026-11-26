using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 query listing pass serial numbers registered to a device that changed after an optional tag.</summary>
/// <param name="DeviceLibraryIdentifier">Apple Wallet deviceLibraryIdentifier path parameter.</param>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier path parameter.</param>
/// <param name="PassesUpdatedSinceTag">Optional passesUpdatedSince query string tag.</param>
public sealed record GetPassKitUpdatableSerialsQuery(
    string DeviceLibraryIdentifier,
    string PassTypeIdentifier,
    string? PassesUpdatedSinceTag = null
) : IQuery<PassKitSerialNumbersResponse>;
