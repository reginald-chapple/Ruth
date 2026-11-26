namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Input specification for creating or updating a BookableService (1-on-1 coaching, counseling, tutoring, intake, or workshop).</summary>
/// <param name="Name">Service display title.</param>
/// <param name="Slug">Unique kebab-case slug.</param>
/// <param name="DeliveryMode">In-person, virtual room, or hybrid delivery mode.</param>
/// <param name="DurationMinutes">Session duration in minutes (5..1440).</param>
/// <param name="SlotIncrementMinutes">Candidate start-time step in minutes (5..480).</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="StandardPrice">Non-negative standard price per participant.</param>
/// <param name="SetupBufferMinutes">Pre-session setup buffer in minutes blocked before start.</param>
/// <param name="TeardownBufferMinutes">Post-session wrap-up/teardown buffer in minutes blocked after end.</param>
/// <param name="MaxParticipantsPerSlot">Maximum concurrent participants per slot (1 for 1-on-1 appointments, &gt; 1 for workshops).</param>
/// <param name="MinimumLeadTimeMinutes">Minimum advance notice in minutes required before slot start.</param>
/// <param name="MaximumAdvanceDays">Maximum future horizon in days open for booking.</param>
/// <param name="CancellationNoticeMinutes">Minimum notice in minutes required for self-service cancellation.</param>
/// <param name="IsFreeForMembers">When true, qualifying members book at zero charge.</param>
/// <param name="MemberOverridePrice">Optional discounted member price per participant when not free.</param>
/// <param name="FreeOrDiscountedMemberGroupId">Optional Groups module Group identifier qualifying for member benefit.</param>
/// <param name="FreeOrDiscountedMemberTier">Optional Wallet Pass / membership tier string qualifying for member benefit.</param>
/// <param name="AllowPunchPassRedemption">Whether active punch pass credits may settle this booking.</param>
/// <param name="PunchPassCreditsRequired">Credits deducted per participant when redeeming a punch pass.</param>
/// <param name="RevenueAccountId">Required Finance FinancialAccount identifier when <paramref name="StandardPrice" /> &gt; 0.</param>
/// <param name="DefaultFinancialGatewayId">Optional default Finance payment gateway identifier.</param>
/// <param name="AllowInvoiceBilling">Whether customers may request an open Finance Invoice.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="DefaultLocationId">Optional default physical Core Location identifier.</param>
/// <param name="AssignedProviderPersonIds">Optional staff/coach/counselor CRM person identifiers delivering this service.</param>
/// <param name="Summary">Optional short summary.</param>
/// <param name="Description">Optional full description.</param>
/// <param name="IsActive">Whether the service is active.</param>
/// <param name="IsPublic">Whether the service is listed in the public Booking Wizard.</param>
/// <param name="Attributes">Optional extensible JSONB attributes.</param>
public sealed record BookableServiceSpec(
    string Name,
    string Slug,
    BookingDeliveryMode DeliveryMode,
    int DurationMinutes,
    int SlotIncrementMinutes,
    string CurrencyCode,
    decimal StandardPrice,
    int SetupBufferMinutes = 0,
    int TeardownBufferMinutes = 0,
    int MaxParticipantsPerSlot = 1,
    int MinimumLeadTimeMinutes = 60,
    int MaximumAdvanceDays = 60,
    int CancellationNoticeMinutes = 1440,
    bool IsFreeForMembers = false,
    decimal? MemberOverridePrice = null,
    Guid? FreeOrDiscountedMemberGroupId = null,
    string? FreeOrDiscountedMemberTier = null,
    bool AllowPunchPassRedemption = false,
    int PunchPassCreditsRequired = 1,
    Guid? RevenueAccountId = null,
    Guid? DefaultFinancialGatewayId = null,
    bool AllowInvoiceBilling = false,
    Guid? BranchId = null,
    Guid? DefaultLocationId = null,
    IReadOnlyList<Guid>? AssignedProviderPersonIds = null,
    string? Summary = null,
    string? Description = null,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
