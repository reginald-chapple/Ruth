namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Identifies whether a booking or availability rule targets a bookable service or a rentable facility/equipment resource.</summary>
public enum BookingTargetKind
{
    /// <summary>1-on-1 appointment, intake, coaching session, or multi-seat paid workshop.</summary>
    Service = 0,

    /// <summary>Physical facility room, court, studio, or equipment rental.</summary>
    Resource = 1,
}
