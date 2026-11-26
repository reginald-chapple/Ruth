using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.CheckIn;

/// <summary>Touch-optimized self-service Check-In and Check-Out Kiosk page.</summary>
public partial class CheckInKiosk : ComponentBase
{
    private readonly Dictionary<
        Guid,
        (Guid HouseholdId, Guid AreaId, Guid? LocationId)
    > _selectedByPerson = [];

    private List<CheckInConfigurationSummaryResponse> _configurations = [];
    private List<ScheduleResponse> _schedules = [];
    private List<KioskFamilyCandidateResponse> _families = [];

    private Guid _selectedConfigId;
    private Guid _selectedScheduleId;
    private bool _isCheckOutMode;
    private string _searchInput = string.Empty;
    private string _sessionNote = string.Empty;
    private CheckInSessionReceiptResponse? _completedReceipt;
    private string? _statusMessage;
    private bool _isError;

    private string SelectedCountText =>
        _selectedByPerson.Count.ToString(CultureInfo.InvariantCulture);

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var configs = await QueryDispatcher.QueryAsync<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>
        >(new GetCheckInConfigurationsQuery(ActiveOnly: true));
        if (configs.IsSuccess && configs.Value.Count > 0)
        {
            _configurations = [.. configs.Value];
            _selectedConfigId = _configurations[0].Id;
        }

        var schedules = await QueryDispatcher.QueryAsync<
            GetSchedulesQuery,
            IReadOnlyList<ScheduleResponse>
        >(new GetSchedulesQuery(ActiveOnly: true));
        if (schedules.IsSuccess && schedules.Value.Count > 0)
        {
            _schedules = [.. schedules.Value];
            _selectedScheduleId = _schedules[0].Id;
        }
    }

    private Task OnConfigurationChangedAsync(Guid configId)
    {
        _selectedConfigId = configId;
        ResetKioskSession();
        return Task.CompletedTask;
    }

    private void SwitchMode(bool checkOutMode)
    {
        _isCheckOutMode = checkOutMode;
        _statusMessage = null;
        _isError = false;
        _completedReceipt = null;
    }

    private void HandleCheckOutOutcome((bool IsError, string Message) outcome)
    {
        _isError = outcome.IsError;
        _statusMessage = outcome.Message;
    }

    private async Task RunSearchAsync()
    {
        _statusMessage = null;
        _isError = false;
        _selectedByPerson.Clear();

        if (_selectedConfigId == Guid.Empty || _selectedScheduleId == Guid.Empty)
        {
            _isError = true;
            _statusMessage = "Select an active check-in configuration and schedule first.";
            return;
        }

        var result = await QueryDispatcher.QueryAsync<
            KioskSearchCandidatesQuery,
            KioskSearchResponse
        >(new KioskSearchCandidatesQuery(_selectedConfigId, _selectedScheduleId, _searchInput));

        if (!result.IsSuccess)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _families = [.. result.Value.Families];
        PreselectRecentAreas();
    }

    private void PreselectRecentAreas()
    {
        foreach (var family in _families)
        {
            foreach (var member in family.Members)
            {
                if (!member.AlreadyCheckedIn && member.PreselectedAreaId.HasValue)
                {
                    _selectedByPerson[member.PersonId] = (
                        family.HouseholdId,
                        member.PreselectedAreaId.Value,
                        member.PreselectedLocationId
                    );
                }
            }
        }
    }

    private bool IsSelected(Guid personId, Guid areaId)
    {
        return _selectedByPerson.TryGetValue(personId, out var current) && current.AreaId == areaId;
    }

    private void ToggleSelection(Guid householdId, Guid personId, Guid areaId, Guid? locationId)
    {
        if (IsSelected(personId, areaId))
        {
            _ = _selectedByPerson.Remove(personId);
            return;
        }

        _selectedByPerson[personId] = (householdId, areaId, locationId);
    }

    private async Task CompleteCheckInAsync()
    {
        if (_selectedByPerson.Count == 0)
        {
            return;
        }

        var householdId = _selectedByPerson.Values.First().HouseholdId;
        var items = _selectedByPerson
            .Select(kvp => new CheckInSelectionItemDto(
                kvp.Key,
                kvp.Value.AreaId,
                kvp.Value.LocationId,
                _selectedScheduleId,
                AttendanceNote: string.IsNullOrWhiteSpace(_sessionNote) ? null : _sessionNote
            ))
            .ToList();

        var result = await CommandDispatcher.SendAsync<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse
        >(
            new SubmitCheckInSessionCommand(
                _selectedConfigId,
                _selectedScheduleId,
                items,
                CheckInSessionChannel.Kiosk,
                HouseholdId: householdId
            )
        );

        if (result.IsSuccess)
        {
            _completedReceipt = result.Value;
            _isError = false;
            _statusMessage = "Check-in completed.";
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private void ResetKioskSession()
    {
        _completedReceipt = null;
        _families = [];
        _selectedByPerson.Clear();
        _searchInput = string.Empty;
        _sessionNote = string.Empty;
        _statusMessage = null;
        _isError = false;
    }
}
