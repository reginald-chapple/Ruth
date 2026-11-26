using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Checks out one or more active check-in entries, verifying security pickup codes unless staff-overridden.</summary>
/// <param name="ConfigurationId">Check-in configuration identifier.</param>
/// <param name="EntryIds">Specific entry identifiers to check out (when empty, checks out all active entries matching <paramref name="SecurityPickupCode" />).</param>
/// <param name="SecurityPickupCode">Guardian security pickup code presented at check-out.</param>
/// <param name="CheckedOutByPersonId">Optional guardian or staff person identifier.</param>
/// <param name="ServiceDate">Optional local service date (defaults to current UTC date).</param>
/// <param name="IsStaffOverride">Whether a staff member is bypassing pickup code verification.</param>
/// <param name="OverrideReason">Audit reason required when <paramref name="IsStaffOverride" /> is true.</param>
public sealed record CheckOutAttendanceCommand(
    Guid ConfigurationId,
    IReadOnlyList<Guid>? EntryIds = null,
    string? SecurityPickupCode = null,
    Guid? CheckedOutByPersonId = null,
    DateOnly? ServiceDate = null,
    bool IsStaffOverride = false,
    string? OverrideReason = null
) : ICommand<CheckOutResultResponse>;
