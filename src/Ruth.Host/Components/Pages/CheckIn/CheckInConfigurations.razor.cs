using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.CheckIn;

/// <summary>Staff management screen for Check-In Configuration profiles, eligibility rules, and room matrices.</summary>
public partial class CheckInConfigurations : ComponentBase
{
    private static readonly CheckInProfileKind[] _profileKinds =
        Enum.GetValues<CheckInProfileKind>();
    private static readonly CheckInSearchMode[] _searchModes = Enum.GetValues<CheckInSearchMode>();
    private static readonly RoomBalancingStrategy[] _balancingStrategies =
        Enum.GetValues<RoomBalancingStrategy>();

    private List<CheckInConfigurationSummaryResponse> _configurations = [];
    private CheckInConfigurationDetailResponse? _selectedDetail;

    private string _configName = string.Empty;
    private string _description = string.Empty;
    private CheckInProfileKind _profileKind = CheckInProfileKind.MemberFacilityAccess;
    private CheckInSearchMode _searchMode = CheckInSearchMode.PhoneAndName;
    private bool _preventDuplicateCheckIn = true;
    private bool _allowCheckout = true;

    private string _areaName = string.Empty;
    private RoomBalancingStrategy _balancingStrategy = RoomBalancingStrategy.LowestCapacityRatio;
    private string _minAgeText = string.Empty;
    private string _maxAgeText = string.Empty;
    private string _tiersCsv = string.Empty;
    private string _skillsCsv = string.Empty;
    private string _roomName = string.Empty;
    private string _softCapText = string.Empty;
    private string _firmCapText = string.Empty;
    private bool _issuePickupCode;
    private bool _supportsAccommodations;

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadConfigurationsAsync();
    }

    private async Task LoadConfigurationsAsync()
    {
        var result = await QueryDispatcher.QueryAsync<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>
        >(new GetCheckInConfigurationsQuery());
        if (result.IsSuccess)
        {
            _configurations = [.. result.Value];
        }
    }

    private async Task CreateConfigurationAsync()
    {
        _statusMessage = null;
        _isError = false;
        var result = await CommandDispatcher.SendAsync<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(
            new CreateCheckInConfigurationCommand(
                _configName,
                Description: _description,
                ProfileKind: _profileKind,
                SearchMode: _searchMode,
                PreventDuplicateCheckIn: _preventDuplicateCheckIn,
                AllowCheckout: _allowCheckout
            )
        );

        if (result.IsSuccess)
        {
            _configName = string.Empty;
            _description = string.Empty;
            _selectedDetail = result.Value;
            _statusMessage = $"Created configuration '{result.Value.Name}'.";
            await LoadConfigurationsAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task SelectConfigurationAsync(Guid id)
    {
        var result = await QueryDispatcher.QueryAsync<
            GetCheckInConfigurationByIdQuery,
            CheckInConfigurationDetailResponse
        >(new GetCheckInConfigurationByIdQuery(id));
        if (result.IsSuccess)
        {
            _selectedDetail = result.Value;
        }
    }

    private async Task AddAreaAsync()
    {
        if (_selectedDetail is null)
        {
            return;
        }

        var eligibility = new CheckInEligibilityRuleDto(
            MinAgeYears: ParseOptionalDecimal(_minAgeText),
            MaxAgeYears: ParseOptionalDecimal(_maxAgeText),
            AllowedMembershipTiers: SplitCsv(_tiersCsv),
            RequiredSkillOrBadgeCodes: SplitCsv(_skillsCsv),
            SupportsAccommodations: _supportsAccommodations
        );
        IReadOnlyList<CheckInRoomMatrixCellInputDto> rooms = string.IsNullOrWhiteSpace(_roomName)
            ? []
            :
            [
                new CheckInRoomMatrixCellInputDto(
                    Guid.CreateVersion7(),
                    _roomName,
                    [Guid.CreateVersion7()],
                    ParseOptionalInt(_softCapText),
                    ParseOptionalInt(_firmCapText)
                ),
            ];

        var result = await CommandDispatcher.SendAsync<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(
            new UpsertCheckInAreaCommand(
                _selectedDetail.Id,
                _areaName,
                BalancingStrategy: _balancingStrategy,
                IssueSecurityPickupCode: _issuePickupCode,
                Eligibility: eligibility,
                Rooms: rooms
            )
        );
        if (result.IsSuccess)
        {
            _areaName = string.Empty;
            _roomName = string.Empty;
            _selectedDetail = result.Value;
            _statusMessage = "Saved check-in area and room matrix.";
            _isError = false;
            await LoadConfigurationsAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task ToggleRoomAsync(Guid areaId, CheckInRoomMatrixCellResponse room)
    {
        if (_selectedDetail is null)
        {
            return;
        }

        var result = await CommandDispatcher.SendAsync<
            SetRoomCheckInAvailabilityCommand,
            CheckInConfigurationDetailResponse
        >(
            new SetRoomCheckInAvailabilityCommand(
                _selectedDetail.Id,
                areaId,
                room.LocationId,
                !room.IsOpenForCheckIn,
                room.SoftCapacity,
                room.FirmCapacity
            )
        );
        if (result.IsSuccess)
        {
            _selectedDetail = result.Value;
            _isError = false;
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private static decimal? ParseOptionalDecimal(string? raw)
    {
        return decimal.TryParse(raw, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static int? ParseOptionalInt(string? raw)
    {
        return int.TryParse(raw, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string[] SplitCsv(string? csv)
    {
        return string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }
}
