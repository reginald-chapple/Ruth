namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Input specification for creating or updating a RentableResource (facility room, court, studio, or equipment).</summary>
/// <param name="Name">Resource display name.</param>
/// <param name="Slug">Unique kebab-case slug.</param>
/// <param name="ResourceKind">Room, court/field, studio, or equipment classification.</param>
/// <param name="CoreLocationId">Linked Core module Location identifier.</param>
/// <param name="MinDurationMinutes">Minimum rental duration in minutes (&gt;= 15).</param>
/// <param name="MaxDurationMinutes">Maximum rental duration in minutes (&gt;= MinDurationMinutes).</param>
/// <param name="SlotIncrementMinutes">Slot start-time increment in minutes (&gt;= 5).</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="HourlyRate">Non-negative standard hourly rate.</param>
/// <param name="FlatCleaningOrSetupFee">Non-negative flat cleaning or setup fee per booking.</param>
/// <param name="QuantityAvailable">Concurrent bookable units (1 for a unique room/court, &gt;= 1 for pooled equipment).</param>
/// <param name="SetupBufferMinutes">Pre-rental setup buffer in minutes.</param>
/// <param name="TeardownBufferMinutes">Post-rental teardown/turnover buffer in minutes.</param>
/// <param name="MinimumLeadTimeMinutes">Minimum advance notice in minutes.</param>
/// <param name="MaximumAdvanceDays">Maximum booking horizon in days.</param>
/// <param name="CancellationNoticeMinutes">Minimum notice in minutes for self-service cancellation.</param>
/// <param name="IsFreeForMembers">When true, qualifying members reserve this resource free of hourly and setup charges.</param>
/// <param name="MemberHourlyRate">Optional discounted member hourly rate when not free.</param>
/// <param name="FreeOrDiscountedMemberGroupId">Optional qualifying Groups module Group identifier.</param>
/// <param name="FreeOrDiscountedMemberTier">Optional qualifying membership tier string.</param>
/// <param name="RevenueAccountId">Required Finance FinancialAccount identifier when paid rates are configured.</param>
/// <param name="DefaultFinancialGatewayId">Optional default Finance payment gateway identifier.</param>
/// <param name="AllowInvoiceBilling">Whether renters may request an open Finance Invoice.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="Description">Optional resource description and rules.</param>
/// <param name="IsActive">Whether the resource is active.</param>
/// <param name="IsPublic">Whether the resource appears in the public Booking Wizard.</param>
/// <param name="Attributes">Optional extensible JSONB attributes.</param>
public sealed record RentableResourceSpec(
    string Name,
    string Slug,
    RentableResourceKind ResourceKind,
    Guid CoreLocationId,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    int SlotIncrementMinutes,
    string CurrencyCode,
    decimal HourlyRate,
    decimal FlatCleaningOrSetupFee = 0m,
    int QuantityAvailable = 1,
    int SetupBufferMinutes = 0,
    int TeardownBufferMinutes = 0,
    int MinimumLeadTimeMinutes = 60,
    int MaximumAdvanceDays = 90,
    int CancellationNoticeMinutes = 1440,
    bool IsFreeForMembers = false,
    decimal? MemberHourlyRate = null,
    Guid? FreeOrDiscountedMemberGroupId = null,
    string? FreeOrDiscountedMemberTier = null,
    Guid? RevenueAccountId = null,
    Guid? DefaultFinancialGatewayId = null,
    bool AllowInvoiceBilling = true,
    Guid? BranchId = null,
    string? Description = null,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
