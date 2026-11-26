using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Updates an existing rentable facility or equipment resource.</summary>
/// <param name="ResourceId">Target resource identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateRentableResourceCommand(Guid ResourceId, RentableResourceSpec Spec)
    : ICommand<RentableResourceResponse>;
