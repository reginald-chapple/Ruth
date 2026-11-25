using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Gets a single device by identifier.</summary>
public sealed record GetDeviceByIdQuery(Guid DeviceId) : IQuery<DeviceResponse>;
