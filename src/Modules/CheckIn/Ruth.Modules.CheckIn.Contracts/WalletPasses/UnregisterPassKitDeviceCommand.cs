using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 command unregistering a device when a user deletes a pass from Apple Wallet.</summary>
/// <param name="DeviceLibraryIdentifier">Apple Wallet deviceLibraryIdentifier path parameter.</param>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier path parameter.</param>
/// <param name="SerialNumber">Issued pass serial number path parameter.</param>
/// <param name="ApplePassAuthToken">Raw token extracted from Authorization: ApplePass header.</param>
public sealed record UnregisterPassKitDeviceCommand(
    string DeviceLibraryIdentifier,
    string PassTypeIdentifier,
    string SerialNumber,
    string? ApplePassAuthToken
) : ICommand;
