using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Command to promote a saved payment method as the constituent's default payment method.</summary>
/// <param name="SavedPaymentMethodId">Target saved payment method identifier.</param>
public sealed record SetDefaultSavedPaymentMethodCommand(Guid SavedPaymentMethodId)
    : ICommand<SavedPaymentMethodResponse>;
