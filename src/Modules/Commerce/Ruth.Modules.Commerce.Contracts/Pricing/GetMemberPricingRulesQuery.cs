using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Queries configured member pricing rules.</summary>
/// <param name="ActiveOnly">When true, returns only active rules.</param>
public sealed record GetMemberPricingRulesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<MemberPricingRuleResponse>>;
