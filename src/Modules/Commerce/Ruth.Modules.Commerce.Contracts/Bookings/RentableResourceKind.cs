namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Classification of a rentable physical resource.</summary>
public enum RentableResourceKind
{
    /// <summary>Meeting room, classroom, auditorium, or conference hall.</summary>
    Room = 0,

    /// <summary>Athletic court, field, lane, or pavilion.</summary>
    CourtOrField = 1,

    /// <summary>Recording, rehearsal, fitness, or maker studio.</summary>
    Studio = 2,

    /// <summary>A/V kit, vehicle, instrument, or pooled equipment item.</summary>
    Equipment = 3,
}
