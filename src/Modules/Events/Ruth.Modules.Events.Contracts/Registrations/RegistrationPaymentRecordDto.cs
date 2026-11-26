namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Recorded deposit, installment, or full payment linked to a Finance FinancialTransaction.</summary>
public sealed record RegistrationPaymentRecordDto(
    Guid Id,
    Guid FinancialTransactionId,
    decimal Amount,
    DateTimeOffset PaidAtUtc,
    string? Note
);
