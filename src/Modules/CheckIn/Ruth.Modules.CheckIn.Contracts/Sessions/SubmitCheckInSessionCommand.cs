using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Creates an atomic check-in session, issuing collision-free daily pickup codes and recording room/group attendance.</summary>
/// <param name="ConfigurationId">Target check-in configuration identifier.</param>
/// <param name="ScheduleId">Default schedule identifier when not specified per item.</param>
/// <param name="Items">Selected participant check-in items.</param>
/// <param name="Channel">Initiating channel (Kiosk, Mobile, or StaffOverride).</param>
/// <param name="HouseholdId">Optional household identifier for family grouping.</param>
/// <param name="CheckedInByPersonId">Optional adult/guardian or staff person identifier.</param>
/// <param name="KioskDeviceId">Optional Core Device identifier of the kiosk station.</param>
/// <param name="ServiceDate">Optional local service date (defaults to current UTC date).</param>
/// <param name="SearchModeUsed">Optional search mode used to locate the household.</param>
/// <param name="SearchTermSummary">Optional masked/summary search context.</param>
/// <param name="ClientIpAddress">Optional client IP address.</param>
public sealed record SubmitCheckInSessionCommand(
    Guid ConfigurationId,
    Guid ScheduleId,
    IReadOnlyList<CheckInSelectionItemDto> Items,
    CheckInSessionChannel Channel = CheckInSessionChannel.Kiosk,
    Guid? HouseholdId = null,
    Guid? CheckedInByPersonId = null,
    Guid? KioskDeviceId = null,
    DateOnly? ServiceDate = null,
    CheckInSearchMode? SearchModeUsed = null,
    string? SearchTermSummary = null,
    string? ClientIpAddress = null
) : ICommand<CheckInSessionReceiptResponse>;
