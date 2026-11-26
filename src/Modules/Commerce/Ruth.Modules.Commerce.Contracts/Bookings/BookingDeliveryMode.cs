namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Delivery mode for a bookable service or rental session.</summary>
public enum BookingDeliveryMode
{
    /// <summary>On-site physical session at a Core Location.</summary>
    InPerson = 0,

    /// <summary>Remote session provisioned via an embedded LiveKit virtual room.</summary>
    VirtualRoom = 1,

    /// <summary>Simultaneous physical room and virtual room participation.</summary>
    Hybrid = 2,
}
