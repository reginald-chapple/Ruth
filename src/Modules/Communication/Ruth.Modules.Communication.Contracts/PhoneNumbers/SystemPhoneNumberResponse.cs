namespace Ruth.Modules.Communication.Contracts.PhoneNumbers;

/// <summary>Read model representing an organization-owned SMS or voice forwarding phone number.</summary>
/// <param name="Id">Unique system phone number identifier.</param>
/// <param name="SystemGuid">Deterministic system GUID.</param>
/// <param name="Name">Human-readable label (e.g., Main Member Support SMS).</param>
/// <param name="Number">Normalized E.164 phone number (e.g., +15552345678).</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsSmsEnabled">Whether outbound/inbound SMS is enabled on this number.</param>
/// <param name="IsSmsForwardingEnabled">Whether inbound SMS messages forward to the assigned person or mobile number.</param>
/// <param name="AssignedToPersonId">Optional CRM person responsible for inbound replies.</param>
/// <param name="MobileApplicationSiteId">Optional linked mobile/web site identifier.</param>
/// <param name="ProviderIdentifier">Optional carrier/Twilio SID.</param>
/// <param name="IsActive">Whether this number is active.</param>
public sealed record SystemPhoneNumberResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string Number,
    string? Description,
    bool IsSmsEnabled,
    bool IsSmsForwardingEnabled,
    Guid? AssignedToPersonId,
    Guid? MobileApplicationSiteId,
    string? ProviderIdentifier,
    bool IsActive
);
