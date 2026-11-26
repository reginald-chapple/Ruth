using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Registers or refreshes a PersonalDevice push notification token (idempotent by token).</summary>
/// <param name="Platform">Client device platform.</param>
/// <param name="PushRegistrationToken">APNs/FCM/WebPush registration token.</param>
/// <param name="PersonId">Optional linked CRM person identifier.</param>
/// <param name="DeviceName">Optional device label.</param>
/// <param name="DeviceVersion">Optional OS or app version.</param>
/// <param name="NotificationsEnabled">Whether push notifications are enabled on this device.</param>
public sealed record RegisterPersonalDeviceCommand(
    PersonalDevicePlatform Platform,
    string PushRegistrationToken,
    Guid? PersonId = null,
    string? DeviceName = null,
    string? DeviceVersion = null,
    bool NotificationsEnabled = true
) : ICommand<PersonalDeviceResponse>;
