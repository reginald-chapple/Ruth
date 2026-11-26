using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Updates an existing member pricing rule.</summary>
/// <param name="RuleId">Target rule identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateMemberPricingRuleCommand(Guid RuleId, MemberPricingRuleSpec Spec)
    : ICommand<MemberPricingRuleResponse>;
