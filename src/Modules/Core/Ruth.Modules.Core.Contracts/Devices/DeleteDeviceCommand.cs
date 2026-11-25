using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Deletes a physical device.</summary>
public sealed record DeleteDeviceCommand(Guid DeviceId) : ICommand<bool>;
