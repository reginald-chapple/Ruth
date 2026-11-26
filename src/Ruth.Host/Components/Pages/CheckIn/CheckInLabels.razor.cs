using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.CheckIn;

/// <summary>Visual Badge and Zebra ZPL II Label Designer with WebUSB and browser print interop.</summary>
public sealed partial class CheckInLabels : ComponentBase, IAsyncDisposable
{
    private static readonly CheckInLabelKind[] _labelKinds = Enum.GetValues<CheckInLabelKind>();
    private static readonly LabelDpi[] _dpis = Enum.GetValues<LabelDpi>();

    private List<CheckInLabelResponse> _labels = [];
    private IJSObjectReference? _printModule;

    private Guid? _editingLabelId;
    private string _name = "Custom Youth Safety Badge";
    private string _key = "custom-youth-badge";
    private string _description = string.Empty;
    private CheckInLabelKind _labelKind = CheckInLabelKind.ParticipantBadge;
    private LabelDpi _dpi = LabelDpi.Dpi203;
    private string _customZplOverride = string.Empty;
    private List<CheckInLabelElementDto> _elements = CreateStarterElements();

    private string _sampleNickName = "Maya";
    private string _sampleLastName = "Rivera";
    private string _samplePickupCode = "BC47";
    private string _sampleRoomName = "STEM Room 101";
    private string _sampleAllergyAlert = "Peanuts - EpiPen";
    private string _sampleTier = "Family";

    private CheckInLabelPreviewResponse? _preview;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadLabelsAsync();
        await CompilePreviewAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_printModule is not null)
        {
            try
            {
                await _printModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore circuit disconnect during teardown.
            }
        }
    }

    private async Task LoadLabelsAsync()
    {
        var result = await QueryDispatcher.QueryAsync<
            GetCheckInLabelsQuery,
            IReadOnlyList<CheckInLabelResponse>
        >(new GetCheckInLabelsQuery());
        if (result.IsSuccess)
        {
            _labels = [.. result.Value];
        }
    }

    private async Task LoadLabelIntoDesignerAsync(CheckInLabelResponse label)
    {
        _editingLabelId = label.Id;
        _name = label.Name;
        _key = label.Key;
        _description = label.Description ?? string.Empty;
        _labelKind = label.LabelKind;
        _dpi = label.Layout.Dpi;
        _elements = [.. label.Layout.Elements];
        _customZplOverride = label.CustomZplOverride ?? string.Empty;
        await CompilePreviewAsync();
    }

    private void ResetForm()
    {
        _editingLabelId = null;
        _name = "Custom Youth Safety Badge";
        _key = "custom-youth-badge";
        _description = string.Empty;
        _labelKind = CheckInLabelKind.ParticipantBadge;
        _dpi = LabelDpi.Dpi203;
        _elements = CreateStarterElements();
        _customZplOverride = string.Empty;
    }

    private async Task CompilePreviewAsync()
    {
        var layout = new CheckInLabelLayoutDto(4.0m, 2.0m, _dpi, _elements);
        var context = new CheckInLabelMergeContextDto(
            $"{_sampleNickName} {_sampleLastName}".Trim(),
            _sampleNickName,
            _sampleLastName,
            _samplePickupCode,
            "Youth Programs",
            _sampleRoomName,
            "9:00 AM Session",
            "2026-11-25",
            "08:55 AM",
            string.IsNullOrWhiteSpace(_sampleAllergyAlert) ? null : _sampleAllergyAlert,
            MembershipTier: string.IsNullOrWhiteSpace(_sampleTier) ? null : _sampleTier,
            GuardianReceiptSummary: $"{_sampleNickName} {_sampleLastName} ({_sampleRoomName})",
            BarcodeValue: _samplePickupCode
        );

        var result = await CommandDispatcher.SendAsync<
            PreviewCheckInLabelCommand,
            CheckInLabelPreviewResponse
        >(
            new PreviewCheckInLabelCommand(
                DraftLayout: layout,
                CustomZplOverride: string.IsNullOrWhiteSpace(_customZplOverride)
                    ? null
                    : _customZplOverride,
                Context: context
            )
        );
        if (result.IsSuccess)
        {
            _preview = result.Value;
            _isError = false;
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task SaveLabelAsync()
    {
        var layout = new CheckInLabelLayoutDto(4.0m, 2.0m, _dpi, _elements);
        var zplOverride = string.IsNullOrWhiteSpace(_customZplOverride) ? null : _customZplOverride;

        var result = _editingLabelId.HasValue
            ? await CommandDispatcher.SendAsync<UpdateCheckInLabelCommand, CheckInLabelResponse>(
                new UpdateCheckInLabelCommand(
                    _editingLabelId.Value,
                    _name,
                    _key,
                    _labelKind,
                    layout,
                    _description,
                    CustomZplOverride: zplOverride
                )
            )
            : await CommandDispatcher.SendAsync<CreateCheckInLabelCommand, CheckInLabelResponse>(
                new CreateCheckInLabelCommand(
                    _name,
                    _key,
                    _labelKind,
                    layout,
                    _description,
                    CustomZplOverride: zplOverride
                )
            );

        if (result.IsSuccess)
        {
            _editingLabelId = result.Value.Id;
            _isError = false;
            _statusMessage = $"Saved label '{result.Value.Name}'.";
            await LoadLabelsAsync();
            await CompilePreviewAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task DeleteLabelAsync(Guid labelId)
    {
        var result = await CommandDispatcher.SendAsync<DeleteCheckInLabelCommand, bool>(
            new DeleteCheckInLabelCommand(labelId)
        );
        if (result.IsSuccess)
        {
            _isError = false;
            _statusMessage = "Deleted custom label.";
            await LoadLabelsAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task PrintViaWebUsbAsync()
    {
        if (_preview is null)
        {
            return;
        }

        var module = await EnsurePrintModuleAsync();
        await module.InvokeVoidAsync("printZplViaWebUsb", _preview.CompiledZpl);
        _isError = false;
        _statusMessage = "Dispatched ZPL payload to WebUSB interop.";
    }

    private async Task PrintViaBrowserFallbackAsync()
    {
        if (_preview is null)
        {
            return;
        }

        var module = await EnsurePrintModuleAsync();
        await module.InvokeVoidAsync(
            "printLabelHtmlFallback",
            _preview.LabelName,
            _preview.VisualElements
        );
    }

    private async Task<IJSObjectReference> EnsurePrintModuleAsync()
    {
        _printModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./ruth-label-print.js"
        );
        return _printModule;
    }

    private static List<CheckInLabelElementDto> CreateStarterElements()
    {
        return
        [
            new CheckInLabelElementDto(
                "nick-name",
                LabelElementKind.TextField,
                24,
                24,
                540,
                72,
                FontHeight: 68,
                ContentTemplate: "{{ Participant.NickName }}"
            ),
            new CheckInLabelElementDto(
                "last-name",
                LabelElementKind.TextField,
                24,
                102,
                540,
                40,
                FontHeight: 34,
                ContentTemplate: "{{ Participant.LastName }}"
            ),
            new CheckInLabelElementDto(
                "pickup-code",
                LabelElementKind.SecurityCodeBlock,
                580,
                20,
                208,
                96,
                FontHeight: 56,
                Alignment: LabelTextAlignment.Center,
                InvertPrint: true,
                ContentTemplate: "{{ Session.SecurityPickupCode }}",
                ShowOnlyWhenFieldPresent: "Session.SecurityPickupCode"
            ),
            new CheckInLabelElementDto(
                "room-zone",
                LabelElementKind.TextField,
                24,
                160,
                540,
                36,
                FontHeight: 28,
                ContentTemplate: "{{ Entry.AreaName }} - {{ Entry.LocationName }}"
            ),
            new CheckInLabelElementDto(
                "alert-banner",
                LabelElementKind.AlertBanner,
                24,
                220,
                764,
                56,
                FontHeight: 28,
                InvertPrint: true,
                ContentTemplate: "ALERT: {{ Alerts.Combined }}",
                ShowOnlyWhenFieldPresent: "Alerts.Combined"
            ),
        ];
    }
}
