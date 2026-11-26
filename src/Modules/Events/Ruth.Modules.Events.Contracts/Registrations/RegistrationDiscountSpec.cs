namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for a promo code or automatic early-bird discount on a RegistrationTemplate.</summary>
public sealed record RegistrationDiscountSpec(
    string Code,
    RegistrationDiscountKind DiscountKind,
    decimal DiscountValue,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? MaxRegistrants = null,
    int? MinRegistrants = null,
    bool IsAutoApplyEarlyBird = false
);
