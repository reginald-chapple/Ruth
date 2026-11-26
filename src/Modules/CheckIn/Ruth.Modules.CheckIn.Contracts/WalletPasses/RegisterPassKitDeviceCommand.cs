using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Apple PassKit WebService v1 command registering an iOS/watchOS device for pass push updates.</summary>
/// <param name="DeviceLibraryIdentifier">Apple Wallet deviceLibraryIdentifier path parameter.</param>
/// <param name="PassTypeIdentifier">PassKit passTypeIdentifier path parameter.</param>
/// <param name="SerialNumber">Issued pass serial number path parameter.</param>
/// <param name="PushToken">APNs pushToken from the PassKit JSON request body.</param>
/// <param name="ApplePassAuthToken">Raw token extracted from Authorization: ApplePass header.</param>
public sealed record RegisterPassKitDeviceCommand(
    string DeviceLibraryIdentifier,
    string PassTypeIdentifier,
    string SerialNumber,
    string PushToken,
    string? ApplePassAuthToken
) : ICommand<bool>;
