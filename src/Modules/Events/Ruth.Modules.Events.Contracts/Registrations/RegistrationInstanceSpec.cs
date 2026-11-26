namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for creating or updating a concrete RegistrationInstance.</summary>
public sealed record RegistrationInstanceSpec(
    Guid TemplateId,
    string Name,
    string Slug,
    int? MaxAttendees = null,
    DateTimeOffset? RegistrationOpensUtc = null,
    DateTimeOffset? RegistrationClosesUtc = null,
    DateOnly? PaymentDeadlineDate = null,
    decimal? CostOverridePerRegistrant = null,
    decimal? DepositOverridePerRegistrant = null,
    Guid? EventItemOccurrenceId = null,
    Guid? LinkedTargetGroupId = null,
    Guid? BranchId = null,
    string? ContactEmail = null,
    string? AdditionalInstructions = null,
    bool IsActive = true
);
