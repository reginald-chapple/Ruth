namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Read model representing a member or visitor personal device registered for push notifications.</summary>
/// <param name="Id">Unique personal device identifier.</param>
/// <param name="PersonId">Optional linked CRM person identifier.</param>
/// <param name="Platform">Device operating system or browser platform.</param>
/// <param name="PushRegistrationToken">Provider push registration token.</param>
/// <param name="DeviceName">Optional user-friendly device model or label.</param>
/// <param name="DeviceVersion">Optional OS or app version string.</param>
/// <param name="NotificationsEnabled">Whether push notifications are currently opted-in.</param>
/// <param name="IsActive">Whether the registration token is active.</param>
/// <param name="LastSeenOnUtc">UTC timestamp of most recent registration heartbeat.</param>
public sealed record PersonalDeviceResponse(
    Guid Id,
    Guid? PersonId,
    PersonalDevicePlatform Platform,
    string PushRegistrationToken,
    string? DeviceName,
    string? DeviceVersion,
    bool NotificationsEnabled,
    bool IsActive,
    DateTimeOffset LastSeenOnUtc
);
