using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to submit an event registration with dynamic form validation, seat hold redemption, discount application, and Finance payment/invoice settlement.</summary>
public sealed record SubmitRegistrationCommand(
    string InstanceIdOrSlug,
    string RegistrarFirstName,
    string RegistrarLastName,
    string RegistrarEmail,
    IReadOnlyList<RegistrantSubmissionDto> Registrants,
    Guid? RegistrarPersonId = null,
    string? SessionToken = null,
    string? DiscountCode = null,
    decimal? InitialPaymentAmount = null,
    string? OneTimeHostedPaymentToken = null,
    Guid? SavedPaymentMethodId = null,
    bool RequestInvoiceBilling = false
) : ICommand<RegistrationResponse>;
