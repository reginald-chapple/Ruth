using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Queries rentable facility and equipment resources.</summary>
/// <param name="ActiveOnly">When true, returns only active resources.</param>
/// <param name="PublicOnly">When true, returns only public resources.</param>
/// <param name="BranchId">Optional Branch/Site filter.</param>
public sealed record GetRentableResourcesQuery(
    bool ActiveOnly = false,
    bool PublicOnly = false,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<RentableResourceResponse>>;
