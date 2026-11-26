using Ruth.Modules.Commerce.Contracts.Catalog;

namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Read projection representing an automatic member pricing discount rule.</summary>
/// <param name="Id">Unique rule identifier.</param>
/// <param name="Name">Rule display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="DiscountKind">Percentage or fixed per-unit discount mode.</param>
/// <param name="DiscountValue">Discount value.</param>
/// <param name="RequiredMembershipTier">Optional required membership tier.</param>
/// <param name="RequiredGroupId">Optional required Group identifier.</param>
/// <param name="RequiredGroupTypeId">Optional required GroupType identifier.</param>
/// <param name="AppliesToProductId">Optional target Product identifier.</param>
/// <param name="AppliesToProductKind">Optional target ProductKind.</param>
/// <param name="Priority">Tie-breaker priority.</param>
/// <param name="StartDate">Optional start date.</param>
/// <param name="EndDate">Optional end date.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record MemberPricingRuleResponse(
    Guid Id,
    string Name,
    string? Description,
    MemberDiscountKind DiscountKind,
    decimal DiscountValue,
    string? RequiredMembershipTier,
    Guid? RequiredGroupId,
    Guid? RequiredGroupTypeId,
    Guid? AppliesToProductId,
    ProductKind? AppliesToProductKind,
    int Priority,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive,
    DateTimeOffset CreatedOnUtc
);
