namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Fulfillment method chosen for a commerce order.</summary>
public enum CommerceFulfillmentMethod
{
    /// <summary>Digital download or instant punch pass issuance requiring no physical handling.</summary>
    DigitalOrInstant = 0,

    /// <summary>Local constituent pickup at a designated Branch/Site.</summary>
    LocalPickup = 1,

    /// <summary>Flat-rate shipping to a constituent mailing address.</summary>
    FlatRateShipping = 2,
}
