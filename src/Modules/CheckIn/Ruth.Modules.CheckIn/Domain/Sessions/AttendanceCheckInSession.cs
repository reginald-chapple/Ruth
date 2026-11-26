using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Events;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.Sessions;

/// <summary>
/// Aggregate root representing an atomic kiosk, mobile, or staff check-in session,
/// its issued daily security pickup code, and its participant attendance entries.
/// </summary>
public sealed class AttendanceCheckInSession
    : AggregateRoot<AttendanceCheckInSessionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasIntegrationEvents
{
    /// <summary>Canonical entity type key for AttendanceCheckInSession.</summary>
    public const string EntityTypeKeyName = "Ruth.CheckIn.AttendanceCheckInSession";

    private readonly List<CheckInAttendanceEntry> _entries = [];
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    private AttendanceCheckInSession() { }

    private AttendanceCheckInSession(
        AttendanceCheckInSessionId id,
        CheckInConfigurationId configurationId
    )
        : base(id)
    {
        CheckInConfigurationId = configurationId;
    }

    /// <summary>Gets the check-in configuration profile identifier.</summary>
    public CheckInConfigurationId CheckInConfigurationId { get; private set; }

    /// <summary>Gets the snapshot configuration name.</summary>
    public string ConfigurationName { get; private set; } = string.Empty;

    /// <summary>Gets the optional Branch / Site identifier.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the optional kiosk Core Device identifier.</summary>
    public Guid? KioskDeviceId { get; private set; }

    /// <summary>Gets the optional household identifier.</summary>
    public Guid? HouseholdId { get; private set; }

    /// <summary>Gets the optional guardian or staff person who initiated the session.</summary>
    public Guid? CheckedInByPersonId { get; private set; }

    /// <summary>Gets the initiating channel (Kiosk, Mobile, or StaffOverride).</summary>
    public CheckInSessionChannel Channel { get; private set; }

    /// <summary>Gets the optional search mode used at the kiosk.</summary>
    public CheckInSearchMode? SearchModeUsed { get; private set; }

    /// <summary>Gets the optional masked/summary search term.</summary>
    public string? SearchTermSummary { get; private set; }

    /// <summary>Gets the local service date.</summary>
    public DateOnly ServiceDate { get; private set; }

    /// <summary>Gets the session-level daily security pickup code when issued.</summary>
    public string? SecurityPickupCode { get; private set; }

    /// <summary>Gets the express mobile barcode token when created via Mobile pre-check-in.</summary>
    public string? MobileExpressToken { get; private set; }

    /// <summary>Gets the optional client IP address.</summary>
    public string? ClientIpAddress { get; private set; }

    /// <summary>Gets the UTC timestamp when the session completed.</summary>
    public DateTimeOffset CompletedOnUtc { get; private set; }

    /// <summary>Gets the participant attendance entries created in this session.</summary>
    public IReadOnlyCollection<CheckInAttendanceEntry> Entries => _entries.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents =>
        _integrationEvents.AsReadOnly();

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public void ClearIntegrationEvents()
    {
        _integrationEvents.Clear();
    }

    internal static Result<AttendanceCheckInSession> Create(
        CheckInSessionCreationSpec sessionSpec,
        IReadOnlyList<CheckInEntrySpec> entrySpecs
    )
    {
        if (entrySpecs is null || entrySpecs.Count == 0)
        {
            return Result.Failure<AttendanceCheckInSession>(CheckInErrors.SessionItemsRequired);
        }

        var searchSummaryResult = CheckInTextPolicy.NormalizeOptional(
            sessionSpec.SearchTermSummary,
            maxLength: 120
        );
        var ipResult = CheckInTextPolicy.NormalizeOptional(
            sessionSpec.ClientIpAddress,
            maxLength: 64
        );
        var combined = Result.Combine(searchSummaryResult, ipResult);
        if (combined.IsFailure)
        {
            return Result.Failure<AttendanceCheckInSession>(combined.Errors);
        }

        var session = new AttendanceCheckInSession(
            AttendanceCheckInSessionId.Create(),
            sessionSpec.ConfigurationId
        )
        {
            ConfigurationName = sessionSpec.ConfigurationName.Trim(),
            BranchId = NormalizeOptionalGuid(sessionSpec.BranchId),
            KioskDeviceId = NormalizeOptionalGuid(sessionSpec.KioskDeviceId),
            HouseholdId = NormalizeOptionalGuid(sessionSpec.HouseholdId),
            CheckedInByPersonId = NormalizeOptionalGuid(sessionSpec.CheckedInByPersonId),
            Channel = sessionSpec.Channel,
            SearchModeUsed = sessionSpec.SearchModeUsed,
            SearchTermSummary = searchSummaryResult.Value,
            ServiceDate = sessionSpec.ServiceDate,
            SecurityPickupCode = string.IsNullOrWhiteSpace(sessionSpec.SecurityPickupCode)
                ? null
                : sessionSpec.SecurityPickupCode.Trim().ToUpperInvariant(),
            MobileExpressToken = string.IsNullOrWhiteSpace(sessionSpec.MobileExpressToken)
                ? null
                : sessionSpec.MobileExpressToken.Trim().ToUpperInvariant(),
            ClientIpAddress = ipResult.Value,
            CompletedOnUtc = sessionSpec.CompletedOnUtc.ToUniversalTime(),
        };

        var entriesResult = session.AttachInitialEntries(entrySpecs);
        return entriesResult.IsFailure
            ? Result.Failure<AttendanceCheckInSession>(entriesResult.Errors)
            : Result.Success(session);
    }

    internal Result<CheckInAttendanceEntry> CheckOutEntry(
        CheckInAttendanceEntryId entryId,
        DateTimeOffset checkedOutOnUtc,
        Guid? checkedOutByPersonId,
        string? providedPickupCode,
        bool isStaffOverride,
        string? overrideReason
    )
    {
        var entry = _entries.Find(e => e.Id == entryId);
        if (entry is null)
        {
            return Result.Failure<CheckInAttendanceEntry>(CheckInErrors.EntryNotFound);
        }

        var outcome = entry.CheckOut(
            checkedOutOnUtc,
            checkedOutByPersonId,
            providedPickupCode,
            isStaffOverride,
            overrideReason
        );
        if (outcome.IsFailure)
        {
            return Result.Failure<CheckInAttendanceEntry>(outcome.Errors);
        }

        _integrationEvents.Add(
            new ParticipantCheckedOutIntegrationEvent(
                Guid.CreateVersion7(),
                checkedOutOnUtc.ToUniversalTime(),
                Id.Value,
                entry.Id.Value,
                entry.PersonId,
                entry.LocationId,
                entry.ServiceDate,
                entry.CheckedOutByPersonId
            )
        );

        return Result.Success(entry);
    }

    internal Result<CheckInAttendanceEntry> ReassignEntryRoom(
        CheckInAttendanceEntryId entryId,
        CheckInAreaId targetAreaId,
        string targetAreaName,
        Guid targetLocationId,
        string targetLocationName,
        Guid? overrideByPersonId,
        string? reason,
        bool isCapacityOverride
    )
    {
        var entry = _entries.Find(e => e.Id == entryId);
        if (entry is null)
        {
            return Result.Failure<CheckInAttendanceEntry>(CheckInErrors.EntryNotFound);
        }

        var outcome = entry.ReassignRoom(
            targetAreaId,
            targetAreaName,
            targetLocationId,
            targetLocationName,
            overrideByPersonId,
            reason,
            isCapacityOverride
        );
        return outcome.IsFailure
            ? Result.Failure<CheckInAttendanceEntry>(outcome.Errors)
            : Result.Success(entry);
    }

    internal void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (CheckedInByPersonId == retiredPersonId)
        {
            CheckedInByPersonId = survivingPersonId;
        }

        foreach (var entry in _entries)
        {
            entry.RepointPerson(retiredPersonId, survivingPersonId);
        }
    }

    private Result AttachInitialEntries(IReadOnlyList<CheckInEntrySpec> entrySpecs)
    {
        var seenPairs = new HashSet<(Guid PersonId, Guid ScheduleId, CheckInAreaId AreaId)>();
        foreach (var spec in entrySpecs)
        {
            if (!seenPairs.Add((spec.PersonId, spec.ScheduleId, spec.AreaId)))
            {
                return CheckInErrors.DuplicateActiveCheckIn;
            }

            var created = CheckInAttendanceEntry.Create(Id, CheckInConfigurationId, spec);
            if (created.IsFailure)
            {
                return Result.Failure(created.Errors);
            }

            _entries.Add(created.Value);
            _integrationEvents.Add(
                new ParticipantCheckedInIntegrationEvent(
                    Guid.CreateVersion7(),
                    CompletedOnUtc,
                    Id.Value,
                    created.Value.Id.Value,
                    CheckInConfigurationId.Value,
                    created.Value.CheckInAreaId.Value,
                    created.Value.LinkedGroupId,
                    created.Value.PersonId,
                    created.Value.LocationId,
                    created.Value.ScheduleId,
                    BranchId,
                    ServiceDate,
                    Channel,
                    created.Value.SecurityPickupCode
                )
            );
        }

        return Result.Success();
    }

    private static Guid? NormalizeOptionalGuid(Guid? raw)
    {
        return raw is { } id && id != Guid.Empty ? id : null;
    }
}
