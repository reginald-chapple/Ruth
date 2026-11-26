using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Gets a bookable service by ID or slug.</summary>
/// <param name="IdOrSlug">Service GUID or kebab-case slug.</param>
public sealed record GetBookableServiceByIdOrSlugQuery(string IdOrSlug)
    : IQuery<BookableServiceResponse>;
