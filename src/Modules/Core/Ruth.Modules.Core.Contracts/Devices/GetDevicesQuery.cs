using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Lists registered physical devices with optional filtering.</summary>
public sealed record GetDevicesQuery(
    Guid? DeviceTypeValueId = null,
    bool ActiveOnly = false,
    string? SearchTerm = null
) : IQuery<IReadOnlyList<DeviceResponse>>;
