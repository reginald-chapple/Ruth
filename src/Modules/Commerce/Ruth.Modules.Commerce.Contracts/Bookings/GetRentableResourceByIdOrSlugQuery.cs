using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Gets a rentable resource by ID or slug.</summary>
/// <param name="IdOrSlug">Resource GUID or kebab-case slug.</param>
public sealed record GetRentableResourceByIdOrSlugQuery(string IdOrSlug)
    : IQuery<RentableResourceResponse>;
