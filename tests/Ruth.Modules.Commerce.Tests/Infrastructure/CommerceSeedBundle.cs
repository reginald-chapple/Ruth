namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal sealed record CommerceSeedBundle(
    Guid OperatingAccountId,
    Guid CharitableAccountId,
    Guid GatewayId,
    Guid LocationId,
    Guid GroupTypeId,
    Guid GroupId,
    Guid DigitalBinaryFileId,
    Guid MemberPersonId,
    Guid HouseholdPartnerPersonId
);
