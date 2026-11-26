using Ruth.Modules.Commerce.Contracts.Catalog;

namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Input specification for creating or updating an automatic tier or group MemberPricingRule.</summary>
/// <param name="Name">Rule display name (for example Gold Member 20% Off, Chapter Leaders Discount).</param>
/// <param name="DiscountKind">Percentage or fixed per-unit discount calculation.</param>
/// <param name="DiscountValue">Positive discount percentage or per-unit currency amount.</param>
/// <param name="RequiredMembershipTier">Optional membership tier string matched against Wallet Pass or buyer tier.</param>
/// <param name="RequiredGroupId">Optional Groups module Group identifier that buyer must belong to.</param>
/// <param name="RequiredGroupTypeId">Optional Groups module GroupType identifier that buyer must belong to.</param>
/// <param name="AppliesToProductId">Optional specific Product identifier (null applies across eligible catalog items).</param>
/// <param name="AppliesToProductKind">Optional ProductKind filter.</param>
/// <param name="Priority">Tie-breaker priority (lower number wins ties when two rules yield identical savings).</param>
/// <param name="StartDate">Optional inclusive start date.</param>
/// <param name="EndDate">Optional inclusive end date.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsActive">Whether the rule is active.</param>
public sealed record MemberPricingRuleSpec(
    string Name,
    MemberDiscountKind DiscountKind,
    decimal DiscountValue,
    string? RequiredMembershipTier = null,
    Guid? RequiredGroupId = null,
    Guid? RequiredGroupTypeId = null,
    Guid? AppliesToProductId = null,
    ProductKind? AppliesToProductKind = null,
    int Priority = 100,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? Description = null,
    bool IsActive = true
);
