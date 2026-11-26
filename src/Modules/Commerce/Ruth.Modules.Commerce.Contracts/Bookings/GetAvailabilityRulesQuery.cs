using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Queries availability and blackout rules filtered by service, resource, or provider.</summary>
public sealed record GetAvailabilityRulesQuery(
    Guid? BookableServiceId = null,
    Guid? RentableResourceId = null,
    Guid? ProviderPersonId = null
) : IQuery<IReadOnlyList<AvailabilityRuleResponse>>;
