using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Creates a new bookable service.</summary>
/// <param name="Spec">Bookable service specification.</param>
public sealed record CreateBookableServiceCommand(BookableServiceSpec Spec)
    : ICommand<BookableServiceResponse>;
