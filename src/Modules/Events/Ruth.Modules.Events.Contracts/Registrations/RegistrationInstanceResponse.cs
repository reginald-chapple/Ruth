namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a RegistrationInstance including live seat capacity, holds, and waitlist counts.</summary>
public sealed record RegistrationInstanceResponse(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    string Name,
    string Slug,
    string CurrencyCode,
    RegistrationPaymentModel PaymentModel,
    decimal EffectiveBaseCostPerRegistrant,
    decimal EffectiveMinimumDepositPerRegistrant,
    bool WaitlistEnabled,
    int SeatHoldTtlMinutes,
    int? MaxAttendees,
    int ConfirmedCount,
    int ActiveHeldSeatCount,
    int WaitlistedCount,
    int? RemainingConfirmedSeats,
    bool IsOpenForRegistration,
    DateTimeOffset? RegistrationOpensUtc,
    DateTimeOffset? RegistrationClosesUtc,
    DateOnly? PaymentDeadlineDate,
    Guid? EventItemOccurrenceId,
    Guid? LinkedTargetGroupId,
    Guid? BranchId,
    string? ContactEmail,
    string? AdditionalInstructions,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
