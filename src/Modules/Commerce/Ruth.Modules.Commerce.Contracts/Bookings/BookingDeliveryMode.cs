namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Delivery mode for a bookable service or rental session.</summary>
public enum BookingDeliveryMode
{
    /// <summary>On-site physical session at a Core Location.</summary>
    InPerson = 0,

    /// <summary>Remote session over phone or external meeting link.</summary>
    Remote = 1,

    /// <summary>Simultaneous physical room and remote participation.</summary>
    Hybrid = 2,
}
