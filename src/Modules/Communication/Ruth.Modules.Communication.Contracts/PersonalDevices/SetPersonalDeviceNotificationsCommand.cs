using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Updates push notification opt-in and active status for a PersonalDevice.</summary>
/// <param name="PersonalDeviceId">Target personal device identifier.</param>
/// <param name="NotificationsEnabled">Whether push notifications are enabled.</param>
/// <param name="IsActive">Whether the device token remains active.</param>
public sealed record SetPersonalDeviceNotificationsCommand(
    Guid PersonalDeviceId,
    bool NotificationsEnabled,
    bool IsActive = true
) : ICommand<PersonalDeviceResponse>;
