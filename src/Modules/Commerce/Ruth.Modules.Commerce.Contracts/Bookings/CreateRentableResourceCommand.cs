using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Creates a new rentable facility or equipment resource.</summary>
/// <param name="Spec">Rentable resource specification.</param>
public sealed record CreateRentableResourceCommand(RentableResourceSpec Spec)
    : ICommand<RentableResourceResponse>;
