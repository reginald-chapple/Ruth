using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Runs registered address verification and geocoding providers against a location.</summary>
public sealed record VerifyLocationAddressCommand(Guid LocationId, bool ReVerify = false)
    : ICommand<LocationResponse>;
