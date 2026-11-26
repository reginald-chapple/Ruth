using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Gets a single booking by confirmation code or GUID.</summary>
/// <param name="CodeOrId">Confirmation code or booking GUID.</param>
public sealed record GetServiceBookingByCodeOrIdQuery(string CodeOrId)
    : IQuery<ServiceBookingResponse>;
