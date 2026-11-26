using Ruth.Modules.Events.Contracts.Registrations;

namespace Ruth.Host.Components.Pages.Events;

internal static class RegistrationTemplateSpecFactory
{
    public static RegistrationTemplateSpec BuildDefaultSpec(
        string name,
        Guid accountId,
        Guid? defaultGatewayId,
        decimal baseCost,
        decimal deposit,
        string? discountCode
    )
    {
        return new RegistrationTemplateSpec(
            name,
            accountId,
            DefaultFinancialGatewayId: defaultGatewayId,
            BaseCostPerRegistrant: baseCost,
            MinimumInitialDepositPerRegistrant: deposit,
            PaymentModel: RegistrationPaymentModel.DepositAndInstallments,
            Forms:
            [
                new RegistrationFormSpec(
                    "Attendee Preferences",
                    Fields:
                    [
                        new RegistrationFormFieldSpec(
                            "dietary_notes",
                            "Dietary Accommodations",
                            RegistrationFieldKind.Text
                        ),
                    ]
                ),
            ],
            Discounts: string.IsNullOrWhiteSpace(discountCode)
                ? null
                :
                [
                    new RegistrationDiscountSpec(
                        discountCode,
                        RegistrationDiscountKind.Percentage,
                        20m
                    ),
                ]
        );
    }
}
