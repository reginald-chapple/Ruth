using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class PreviewCommerceOrderQuoteQueryHandler(
    CommerceCartEvaluationCollaborator evaluator,
    IClock clock
) : IQueryHandler<PreviewCommerceOrderQuoteQuery, CommerceOrderQuoteResponse>
{
    public async Task<Result<CommerceOrderQuoteResponse>> HandleAsync(
        PreviewCommerceOrderQuoteQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var planRes = await evaluator.EvaluateCartAsync(
            query.Items,
            query.BuyerPersonId,
            query.ExplicitMembershipTier,
            query.FulfillmentMethod,
            query.TaxRateOverridePercent,
            trackCatalogEntities: false,
            clock.UtcNow,
            cancellationToken
        );

        return planRes.IsFailure
            ? Result.Failure<CommerceOrderQuoteResponse>(planRes.Errors)
            : CommerceOrderMapper.MapQuote(planRes.Value);
    }
}
