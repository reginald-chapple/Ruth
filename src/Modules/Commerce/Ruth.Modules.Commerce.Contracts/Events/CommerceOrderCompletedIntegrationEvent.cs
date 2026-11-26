using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Commerce.Contracts.Events;

/// <summary>Outbox integration event emitted when a storefront or staff commerce order is settled.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when the order was settled.</param>
/// <param name="OrderId">Unique commerce order identifier.</param>
/// <param name="OrderNumber">Human-readable order number.</param>
/// <param name="BuyerPersonId">Optional buyer CRM person identifier.</param>
/// <param name="BuyerEmail">Buyer contact email address.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="TotalAmount">Total order amount.</param>
/// <param name="NonDeductibleRevenueAmount">Non-deductible commercial operating revenue amount (including tax and shipping).</param>
/// <param name="TaxDeductibleContributionAmount">IRS quid-pro-quo tax-deductible charitable contribution excess.</param>
/// <param name="LinkedFinancialTransactionId">Optional settled financial transaction identifier.</param>
/// <param name="LinkedInvoiceId">Optional issued invoice identifier.</param>
public sealed record CommerceOrderCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid OrderId,
    string OrderNumber,
    Guid? BuyerPersonId,
    string BuyerEmail,
    string CurrencyCode,
    decimal TotalAmount,
    decimal NonDeductibleRevenueAmount,
    decimal TaxDeductibleContributionAmount,
    Guid? LinkedFinancialTransactionId,
    Guid? LinkedInvoiceId
) : IIntegrationEvent;
