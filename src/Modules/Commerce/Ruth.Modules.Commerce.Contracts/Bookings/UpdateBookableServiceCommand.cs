using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Updates an existing bookable service.</summary>
/// <param name="ServiceId">Target service identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateBookableServiceCommand(Guid ServiceId, BookableServiceSpec Spec)
    : ICommand<BookableServiceResponse>;
