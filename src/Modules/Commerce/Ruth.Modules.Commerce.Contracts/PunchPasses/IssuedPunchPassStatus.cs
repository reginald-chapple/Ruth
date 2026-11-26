namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Lifecycle status of an IssuedPunchPass.</summary>
public enum IssuedPunchPassStatus
{
    /// <summary>Active pass with remaining positive credits.</summary>
    Active = 0,

    /// <summary>All granted credits have been redeemed.</summary>
    Depleted = 1,

    /// <summary>Pass validity window has elapsed.</summary>
    Expired = 2,

    /// <summary>Pass revoked due to order refund or administrative action.</summary>
    Revoked = 3,
}
