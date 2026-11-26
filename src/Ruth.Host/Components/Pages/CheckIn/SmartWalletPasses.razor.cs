using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.CheckIn;

/// <summary>Staff and Member Smart Wallet Pass Issuance, Live Update, Visual Preview, and Kiosk Scan Verifier UI.</summary>
public sealed partial class SmartWalletPasses : ComponentBase
{
    private List<WalletPassTemplateResponse> _templates = [];
    private List<IssuedWalletPassResponse> _issuedPasses = [];

    private Guid _selectedTemplateId;
    private string _personIdInput = Guid.CreateVersion7().ToString("D");
    private string _holderName = "Jordan Rivera";
    private string _membershipTier = "Family Plus";
    private string _punchBalanceInput = "12";
    private string _nextShiftSummary = "Sat 9:00 AM Welcome Team";
    private string _eventTicketSummary = string.Empty;
    private bool _sendClaimNotification;
    private string _recipientEmail = string.Empty;

    private IssuedWalletPassResponse? _selectedPass;
    private string? _previewSvg;
    private string? _lastClaimUrl;
    private string _scanTokenInput = string.Empty;
    private KioskWalletPassScanResponse? _lastScanResult;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ReloadDataAsync();
        RenderDraftSvgPreview();
    }

    private async Task ReloadDataAsync()
    {
        var templatesResult = await QueryDispatcher.QueryAsync<
            GetWalletPassTemplatesQuery,
            IReadOnlyList<WalletPassTemplateResponse>
        >(new GetWalletPassTemplatesQuery(ActiveOnly: true));
        if (templatesResult.IsSuccess)
        {
            _templates = [.. templatesResult.Value];
            if (_selectedTemplateId == Guid.Empty && _templates.Count > 0)
            {
                _selectedTemplateId = _templates[0].Id;
            }
        }

        var passesResult = await QueryDispatcher.QueryAsync<
            GetIssuedWalletPassesQuery,
            IReadOnlyList<IssuedWalletPassResponse>
        >(new GetIssuedWalletPassesQuery());
        if (passesResult.IsSuccess)
        {
            _issuedPasses = [.. passesResult.Value];
        }
    }

    private async Task IssuePassAsync()
    {
        if (!Guid.TryParse(_personIdInput, out var personId) || personId == Guid.Empty)
        {
            _isError = true;
            _statusMessage = "Enter a valid non-empty Participant Person GUID.";
            return;
        }

        var result = await CommandDispatcher.SendAsync<
            IssueWalletPassCommand,
            IssueWalletPassResultResponse
        >(
            new IssueWalletPassCommand(
                _selectedTemplateId,
                personId,
                BuildDynamicFields(),
                SendClaimNotification: _sendClaimNotification,
                RecipientEmail: string.IsNullOrWhiteSpace(_recipientEmail) ? null : _recipientEmail
            )
        );
        if (result.IsFailure)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _selectedPass = result.Value.Pass;
        _lastClaimUrl = $"/api/checkin/wallet-passes/claim/{result.Value.ClaimToken}";
        _scanTokenInput = result.Value.Pass.ScanToken;
        _isError = false;
        _statusMessage =
            $"Issued wallet pass {result.Value.Pass.SerialNumber} ({result.Value.Pass.ScanToken}).";

        await ReloadDataAsync();
        await SelectIssuedPassAsync(result.Value.Pass);
    }

    private async Task SelectIssuedPassAsync(IssuedWalletPassResponse pass)
    {
        _selectedPass = pass;
        _holderName = pass.DynamicFields.HolderDisplayName ?? string.Empty;
        _membershipTier = pass.DynamicFields.MembershipTier ?? string.Empty;
        _punchBalanceInput = pass.DynamicFields.PunchOrCreditBalance?.ToString(
            CultureInfo.InvariantCulture
        ) ?? string.Empty;
        _nextShiftSummary = pass.DynamicFields.NextShiftOrBookingSummary ?? string.Empty;
        _eventTicketSummary = pass.DynamicFields.EventTicketSummary ?? string.Empty;
        _scanTokenInput = pass.ScanToken;

        var bundle = await QueryDispatcher.QueryAsync<
            PreviewIssuedWalletPassQuery,
            WalletPassClaimBundleResponse
        >(new PreviewIssuedWalletPassQuery(pass.Id));
        if (bundle.IsSuccess)
        {
            _previewSvg = bundle.Value.PreviewCard.PreviewSvgMarkup;
        }
    }

    private async Task UpdateSelectedPassFieldsAsync()
    {
        if (_selectedPass is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            UpdateIssuedWalletPassDynamicFieldsCommand,
            IssuedWalletPassResponse
        >(new UpdateIssuedWalletPassDynamicFieldsCommand(_selectedPass.Id, BuildDynamicFields()));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Updated pass {res.Value.SerialNumber} to Revision {res.Value.Revision}."
        );
        await ReloadDataAsync();
        await SelectIssuedPassAsync(res.Value);
    }

    private async Task RevokeSelectedPassAsync()
    {
        if (_selectedPass is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            RevokeIssuedWalletPassCommand,
            IssuedWalletPassResponse
        >(new RevokeIssuedWalletPassCommand(_selectedPass.Id, "Staff UI revocation"));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Revoked pass {res.Value.SerialNumber}.";
        await ReloadDataAsync();
        await SelectIssuedPassAsync(res.Value);
    }

    private async Task VerifyKioskScanAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            ResolveKioskWalletPassScanQuery,
            KioskWalletPassScanResponse
        >(new ResolveKioskWalletPassScanQuery(_scanTokenInput));
        if (res.IsFailure)
        {
            _lastScanResult = null;
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _lastScanResult = res.Value;
        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Kiosk scan verified in {res.Value.ResolutionMilliseconds} ms."
        );
    }

    private WalletPassDynamicFieldsDto BuildDynamicFields()
    {
        int? balance = int.TryParse(
            _punchBalanceInput,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;
        return new WalletPassDynamicFieldsDto(
            HolderDisplayName: _holderName,
            MembershipTier: _membershipTier,
            PunchOrCreditBalance: balance,
            NextShiftOrBookingSummary: _nextShiftSummary,
            EventTicketSummary: _eventTicketSummary
        );
    }

    private void RenderDraftSvgPreview()
    {
        var template = _templates.FirstOrDefault(t => t.Id == _selectedTemplateId);
        var spec = template is not null ? BuildSpecFromTemplate(template) : BuildDefaultDraftSpec();
        _previewSvg = WalletPassSvgCardRenderer.RenderSvg(spec);
    }

    private WalletPassRenderSpecDto BuildSpecFromTemplate(WalletPassTemplateResponse template)
    {
        return new WalletPassRenderSpecDto(
            Guid.Empty,
            "RWP-PREVIEW",
            "WLT-PREVIEW",
            template.PassTypeIdentifier,
            template.OrganizationName,
            template.Name,
            template.Description,
            template.CardStyle,
            template.BackgroundColorHex,
            template.ForegroundColorHex,
            template.LabelColorHex,
            template.LogoText,
            WebServiceBaseUrl: null,
            AppleAuthenticationToken: null,
            DateTimeOffset.UtcNow,
            ExpiresAtUtc: null,
            Revision: 1,
            BuildDynamicFields()
        );
    }

    private WalletPassRenderSpecDto BuildDefaultDraftSpec()
    {
        return new WalletPassRenderSpecDto(
            Guid.Empty,
            "RWP-PREVIEW",
            "WLT-PREVIEW",
            "pass.org.ruth.member",
            "Ruth Community",
            "Standard Member & Access Pass",
            "Preview",
            WalletPassCardStyle.MemberCredential,
            "#1E3A8A",
            "#FFFFFF",
            "#BFDBFE",
            "Ruth Pass",
            WebServiceBaseUrl: null,
            AppleAuthenticationToken: null,
            DateTimeOffset.UtcNow,
            ExpiresAtUtc: null,
            Revision: 1,
            BuildDynamicFields()
        );
    }
}
