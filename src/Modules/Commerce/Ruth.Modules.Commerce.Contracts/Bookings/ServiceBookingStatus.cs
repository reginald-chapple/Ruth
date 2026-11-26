namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Lifecycle status of a ServiceOrResourceBooking.</summary>
public enum ServiceBookingStatus
{
    /// <summary>Booking confirmed (free, paid, punch-pass redeemed, or invoiced).</summary>
    Confirmed = 0,

    /// <summary>Booking cancelled within policy or by staff override.</summary>
    Cancelled = 1,

    /// <summary>Appointment or rental completed.</summary>
    Completed = 2,

    /// <summary>Participant did not attend.</summary>
    NoShow = 3,
}
