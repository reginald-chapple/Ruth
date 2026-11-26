using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class PreviewRegistrationPricingQueryHandler(
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : IQueryHandler<PreviewRegistrationPricingQuery, RegistrationPricingQuoteResponse>
{
    public async Task<Result<RegistrationPricingQuoteResponse>> HandleAsync(
        PreviewRegistrationPricingQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var instance = await loader.LoadInstanceByIdOrSlugAsync(
            query.InstanceIdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        if (instance is null)
        {
            return EventsRegistrationErrors.InstanceNotFound;
        }

        var template = await loader.LoadTemplateAsync(
            instance.TemplateId,
            asNoTracking: true,
            cancellationToken
        );
        if (template is null)
        {
            return EventsRegistrationErrors.TemplateNotFound;
        }

        var planRes = await EvaluateQuoteAsync(template, instance, query, cancellationToken);
        if (planRes.IsFailure)
        {
            return Result.Failure<RegistrationPricingQuoteResponse>(planRes.Errors);
        }

        var p = planRes.Value;
        return new RegistrationPricingQuoteResponse(
            instance.Id.Value,
            template.CurrencyCode,
            p.ConfirmedCount,
            p.WaitlistedCount,
            p.BaseSubtotal,
            p.FeesSubtotal,
            p.DiscountTotal,
            p.AppliedDiscountCode,
            p.TotalAmount,
            p.MinimumInitialAmountDue,
            template.PaymentModel
        );
    }

    private async Task<Result<EvaluatedRegistrationPlan>> EvaluateQuoteAsync(
        RegistrationTemplate template,
        RegistrationInstance instance,
        PreviewRegistrationPricingQuery query,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = timeProvider.GetUtcNow();
        var (confirmed, held, _, maxWait) = await loader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            query.SessionToken,
            cancellationToken
        );
        var feeQuantities = await loader.LoadConsumedFeeItemQuantitiesAsync(
            instance.Id,
            cancellationToken
        );
        var discountUsed = await loader.CountDiscountedRegistrantsAsync(
            instance.Id,
            query.DiscountCode,
            cancellationToken
        );

        return RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            template,
            instance,
            query.Registrants,
            confirmed,
            held,
            maxWait,
            feeQuantities,
            discountUsed,
            query.DiscountCode,
            DateOnly.FromDateTime(nowUtc.UtcDateTime)
        );
    }
}
