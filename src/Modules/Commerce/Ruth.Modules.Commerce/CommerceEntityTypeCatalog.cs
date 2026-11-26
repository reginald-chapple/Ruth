using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Commerce;

internal static class CommerceEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            CommerceSystemGuids.PunchPassType,
            PunchPassType.EntityTypeKeyName,
            nameof(PunchPassType),
            "Punch Pass Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CommerceSystemGuids.Product,
            Product.EntityTypeKeyName,
            nameof(Product),
            "Commerce Product",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CommerceSystemGuids.MemberPricingRule,
            MemberPricingRule.EntityTypeKeyName,
            nameof(MemberPricingRule),
            "Member Pricing Rule",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CommerceSystemGuids.CommerceOrder,
            CommerceOrder.EntityTypeKeyName,
            nameof(CommerceOrder),
            "Commerce Order",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CommerceSystemGuids.IssuedPunchPass,
            IssuedPunchPass.EntityTypeKeyName,
            nameof(IssuedPunchPass),
            "Issued Punch Pass",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CommerceSystemGuids.BookableService,
            BookableService.EntityTypeKeyName,
            nameof(BookableService),
            "Bookable Service",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CommerceSystemGuids.RentableResource,
            RentableResource.EntityTypeKeyName,
            nameof(RentableResource),
            "Rentable Resource",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CommerceSystemGuids.AvailabilityRule,
            AvailabilityRule.EntityTypeKeyName,
            nameof(AvailabilityRule),
            "Booking Availability Rule",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CommerceSystemGuids.ServiceBooking,
            ServiceOrResourceBooking.EntityTypeKeyName,
            nameof(ServiceOrResourceBooking),
            "Service or Resource Booking",
            isSecured: true,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Commerce",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
