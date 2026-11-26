using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Queries bookable services.</summary>
/// <param name="ActiveOnly">When true, returns only active services.</param>
/// <param name="PublicOnly">When true, returns only public services.</param>
/// <param name="BranchId">Optional Branch/Site filter.</param>
public sealed record GetBookableServicesQuery(
    bool ActiveOnly = false,
    bool PublicOnly = false,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<BookableServiceResponse>>;
