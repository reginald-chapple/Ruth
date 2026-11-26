using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to preview seat allocation, fees, promo/early-bird discounts, and minimum deposit due.</summary>
public sealed record PreviewRegistrationPricingQuery(
    string InstanceIdOrSlug,
    IReadOnlyList<RegistrantSubmissionDto> Registrants,
    string? DiscountCode = null,
    string? SessionToken = null
) : IQuery<RegistrationPricingQuoteResponse>;
