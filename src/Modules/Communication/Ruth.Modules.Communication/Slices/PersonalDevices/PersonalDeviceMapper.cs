using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Domain.PersonalDevices;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal static class PersonalDeviceMapper
{
    public static PersonalDeviceResponse ToResponse(PersonalDevice entity)
    {
        return new PersonalDeviceResponse(
            entity.Id.Value,
            entity.PersonId,
            entity.Platform,
            entity.PushRegistrationToken,
            entity.DeviceName,
            entity.DeviceVersion,
            entity.NotificationsEnabled,
            entity.IsActive,
            entity.LastSeenOnUtc
        );
    }
}
