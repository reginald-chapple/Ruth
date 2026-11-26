using Ruth.Modules.Commerce.Contracts.Pricing;

namespace Ruth.Modules.Commerce.Domain.Pricing;

/// <summary>Default flat-rate tax calculator honoring product tax rates and optional jurisdiction/gateway overrides.</summary>
public sealed class FlatRateCommerceTaxCalculator : ICommerceTaxCalculator
{
    public decimal CalculateTax(
        decimal taxableAmount,
        decimal defaultRatePercent,
        decimal? overrideRatePercent = null
    )
    {
        if (taxableAmount <= 0m)
        {
            return 0m;
        }

        var rate = Math.Clamp(overrideRatePercent ?? defaultRatePercent, 0m, 100m);
        return rate <= 0m
            ? 0m
            : decimal.Round(taxableAmount * (rate / 100m), 2, MidpointRounding.AwayFromZero);
    }
}
