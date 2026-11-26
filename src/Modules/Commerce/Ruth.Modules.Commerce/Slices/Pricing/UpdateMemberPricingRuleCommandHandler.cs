using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Catalog;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Pricing;

internal sealed class UpdateMemberPricingRuleCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<UpdateMemberPricingRuleCommand, MemberPricingRuleResponse>
{
    public async Task<Result<MemberPricingRuleResponse>> HandleAsync(
        UpdateMemberPricingRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = MemberPricingRuleId.From(command.RuleId);
        var rule = await dbContext.MemberPricingRules.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (rule is null)
        {
            return CommerceErrors.MemberPricingRuleNotFound;
        }

        var updated = rule.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<MemberPricingRuleResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapRule(rule);
    }
}
