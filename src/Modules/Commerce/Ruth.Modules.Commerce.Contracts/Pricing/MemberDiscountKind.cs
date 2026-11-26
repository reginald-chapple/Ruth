namespace Ruth.Modules.Commerce.Contracts.Pricing;

/// <summary>Calculation mode for a member pricing rule discount.</summary>
public enum MemberDiscountKind
{
    /// <summary>Percentage off the variant list unit price (0 &lt; Value &lt;= 100).</summary>
    Percentage = 0,

    /// <summary>Fixed currency amount deducted per unit.</summary>
    FixedAmountPerUnit = 1,
}
