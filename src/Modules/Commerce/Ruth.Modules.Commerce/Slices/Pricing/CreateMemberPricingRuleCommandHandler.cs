using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Catalog;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Pricing;

internal sealed class CreateMemberPricingRuleCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<CreateMemberPricingRuleCommand, MemberPricingRuleResponse>
{
    public async Task<Result<MemberPricingRuleResponse>> HandleAsync(
        CreateMemberPricingRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = MemberPricingRule.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<MemberPricingRuleResponse>(created.Errors);
        }

        _ = dbContext.MemberPricingRules.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapRule(created.Value);
    }
}
