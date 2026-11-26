namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Lifecycle status of a CommerceOrder.</summary>
public enum CommerceOrderStatus
{
    /// <summary>Order created via invoice billing awaiting settlement.</summary>
    PendingPayment = 0,

    /// <summary>Payment settled; physical items awaiting fulfillment.</summary>
    Paid = 1,

    /// <summary>Physical order staged for constituent pickup.</summary>
    ReadyForPickup = 2,

    /// <summary>Physical order dispatched via flat-rate shipping.</summary>
    Shipped = 3,

    /// <summary>All digital, punch pass, and physical items completely fulfilled.</summary>
    Fulfilled = 4,

    /// <summary>Order partially refunded.</summary>
    PartiallyRefunded = 5,

    /// <summary>Order fully refunded.</summary>
    Refunded = 6,

    /// <summary>Unpaid or unfulfilled order cancelled.</summary>
    Cancelled = 7,
}
