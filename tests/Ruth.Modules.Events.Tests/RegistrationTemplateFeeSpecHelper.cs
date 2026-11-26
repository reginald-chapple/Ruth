using Ruth.Modules.Events.Contracts.Registrations;

namespace Ruth.Modules.Events.Tests;

internal static class RegistrationTemplateFeeSpecHelper
{
    public static RegistrationFeeSpec BuildOptionalWorkshopFee()
    {
        return new RegistrationFeeSpec(
            "Optional Lab Kit",
            RegistrationFeeType.SingleOption,
            IsRequired: false,
            AllowDiscount: true,
            Items:
            [
                new RegistrationFeeItemSpec(
                    "KIT-STD",
                    "Standard Kit",
                    20m,
                    MaxQuantityAvailable: 2
                ),
            ]
        );
    }
}
