namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a promo code or automatic early-bird discount on a RegistrationTemplate.</summary>
public sealed record RegistrationDiscountResponse(
    Guid Id,
    string Code,
    RegistrationDiscountKind DiscountKind,
    decimal DiscountValue,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? MaxRegistrants,
    int? MinRegistrants,
    bool IsAutoApplyEarlyBird
);
