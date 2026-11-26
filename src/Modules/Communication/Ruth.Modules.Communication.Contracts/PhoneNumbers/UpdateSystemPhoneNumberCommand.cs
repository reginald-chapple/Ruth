using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PhoneNumbers;

/// <summary>Updates an organization-owned SystemPhoneNumber.</summary>
/// <param name="SystemPhoneNumberId">Target system phone number identifier.</param>
/// <param name="Name">Human-readable label.</param>
/// <param name="Number">Phone number in E.164 format.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsSmsEnabled">Whether SMS dispatch is enabled.</param>
/// <param name="IsSmsForwardingEnabled">Whether inbound SMS forwarding is enabled.</param>
/// <param name="AssignedToPersonId">Optional CRM person ID assigned to monitor replies.</param>
/// <param name="MobileApplicationSiteId">Optional mobile/web site ID.</param>
/// <param name="ProviderIdentifier">Optional carrier/Twilio phone SID.</param>
/// <param name="IsActive">Whether the phone number is active.</param>
public sealed record UpdateSystemPhoneNumberCommand(
    Guid SystemPhoneNumberId,
    string Name,
    string Number,
    string? Description = null,
    bool IsSmsEnabled = true,
    bool IsSmsForwardingEnabled = false,
    Guid? AssignedToPersonId = null,
    Guid? MobileApplicationSiteId = null,
    string? ProviderIdentifier = null,
    bool IsActive = true
) : ICommand<SystemPhoneNumberResponse>;
