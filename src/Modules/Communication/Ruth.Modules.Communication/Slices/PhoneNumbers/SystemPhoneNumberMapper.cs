using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Domain.PhoneNumbers;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal static class SystemPhoneNumberMapper
{
    public static SystemPhoneNumberResponse ToResponse(SystemPhoneNumber entity)
    {
        return new SystemPhoneNumberResponse(
            entity.Id.Value,
            entity.SystemGuid,
            entity.Name,
            entity.Number,
            entity.Description,
            entity.IsSmsEnabled,
            entity.IsSmsForwardingEnabled,
            entity.AssignedToPersonId,
            entity.MobileApplicationSiteId,
            entity.ProviderIdentifier,
            entity.IsActive
        );
    }
}
