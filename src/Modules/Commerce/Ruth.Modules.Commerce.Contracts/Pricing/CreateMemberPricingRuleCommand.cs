using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Creates a new automatic member pricing rule.</summary>
/// <param name="Spec">Member pricing rule specification.</param>
public sealed record CreateMemberPricingRuleCommand(MemberPricingRuleSpec Spec)
    : ICommand<MemberPricingRuleResponse>;
