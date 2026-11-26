namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Complete read projection for a Registration including registrants, invoice link, and payment ledger.</summary>
public sealed record RegistrationResponse(
    Guid Id,
    Guid InstanceId,
    string ConfirmationCode,
    Guid? RegistrarPersonId,
    string RegistrarFirstName,
    string RegistrarLastName,
    string RegistrarEmail,
    RegistrationStatus Status,
    string CurrencyCode,
    string? AppliedDiscountCode,
    decimal SubtotalAmount,
    decimal DiscountTotalAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    Guid? LinkedInvoiceId,
    IReadOnlyList<RegistrationRegistrantResponse> Registrants,
    IReadOnlyList<RegistrationPaymentRecordDto> Payments,
    DateTimeOffset CreatedOnUtc
);
