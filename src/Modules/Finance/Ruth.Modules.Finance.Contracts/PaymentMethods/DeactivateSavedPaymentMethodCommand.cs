using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Command to deactivate a saved payment method so it is no longer offered for new charges.</summary>
/// <param name="SavedPaymentMethodId">Target saved payment method identifier.</param>
public sealed record DeactivateSavedPaymentMethodCommand(Guid SavedPaymentMethodId)
    : ICommand<SavedPaymentMethodResponse>;
