namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Pluggable tax calculation seam supporting flat product tax rates or external gateway tax hooks without ERP bloat.</summary>
public interface ICommerceTaxCalculator
{
    /// <summary>Calculates rounded sales tax for a commercial taxable base amount.</summary>
    /// <param name="taxableAmount">Non-deductible commercial base amount subject to tax.</param>
    /// <param name="defaultRatePercent">Product-configured flat tax rate percent (0..100).</param>
    /// <param name="overrideRatePercent">Optional checkout or jurisdiction override rate percent.</param>
    /// <returns>Rounded non-negative tax amount.</returns>
    decimal CalculateTax(
        decimal taxableAmount,
        decimal defaultRatePercent,
        decimal? overrideRatePercent = null
    );
}
