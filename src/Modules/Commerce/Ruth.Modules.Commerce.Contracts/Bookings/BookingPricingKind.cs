namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Pricing model resolved for a customer booking.</summary>
public enum BookingPricingKind
{
    /// <summary>Standard list fee or hourly rental rate.</summary>
    StandardPaid = 0,

    /// <summary>Zero-cost benefit granted to qualifying members.</summary>
    MemberFree = 1,

    /// <summary>Discounted member rate applied via tier or group membership.</summary>
    MemberDiscounted = 2,

    /// <summary>Settled by redeeming credits from an active IssuedPunchPass.</summary>
    PunchPassRedeemed = 3,

    /// <summary>Zero-cost public service (such as free intake consultation).</summary>
    Complimentary = 4,
}
