using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to record an installment or remaining-balance payment toward an existing Registration.</summary>
public sealed record RecordRegistrationPaymentCommand(
    Guid RegistrationId,
    decimal Amount,
    string? OneTimeHostedPaymentToken = null,
    Guid? SavedPaymentMethodId = null,
    string? ExternalTransactionCode = null,
    string? Note = null
) : ICommand<RegistrationResponse>;
