using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Deletes a PersonalDevice push registration record.</summary>
/// <param name="PersonalDeviceId">Target personal device identifier.</param>
public sealed record DeletePersonalDeviceCommand(Guid PersonalDeviceId) : ICommand<bool>;
