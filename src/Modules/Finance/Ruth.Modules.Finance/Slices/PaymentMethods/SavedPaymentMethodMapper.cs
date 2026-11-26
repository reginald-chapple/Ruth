using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain.PaymentMethods;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal static class SavedPaymentMethodMapper
{
    public static SavedPaymentMethodResponse ToResponse(
        SavedPaymentMethod method,
        string gatewayName
    )
    {
        return new SavedPaymentMethodResponse(
            method.Id.Value,
            method.PersonId,
            method.HouseholdId,
            method.FinancialGatewayId.Value,
            gatewayName,
            method.MethodKind,
            method.DisplayName,
            method.CardBrand,
            method.AccountNumberMasked,
            method.ExpiryMonth,
            method.ExpiryYear,
            method.IsDefault,
            method.IsActive,
            method.CreatedOnUtc
        );
    }

    public static BankAccountIdentifierResponse ToResponse(BankAccountIdentifier identifier)
    {
        return new BankAccountIdentifierResponse(
            identifier.Id.Value,
            identifier.PersonId,
            identifier.MaskedRoutingNumber,
            identifier.MaskedAccountNumber,
            identifier.BankName,
            identifier.RoutingAndAccountHash,
            identifier.VerifiedOnUtc
        );
    }
}
