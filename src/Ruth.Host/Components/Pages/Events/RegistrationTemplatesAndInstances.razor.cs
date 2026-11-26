using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Events;

/// <summary>Staff workspace for configuring RegistrationTemplates, RegistrationInstances, rosters, and waitlists.</summary>
public sealed partial class RegistrationTemplatesAndInstances : ComponentBase
{
    private List<FinancialAccountResponse> _accounts = [];
    private List<RegistrationTemplateResponse> _templates = [];
    private List<RegistrationInstanceResponse> _instances = [];
    private List<RegistrationResponse> _registrations = [];
    private Guid? _defaultGatewayId;

    private string _templateName = "Annual Leadership Conference";
    private Guid _accountId;
    private string _baseCostInput = "120.00";
    private string _depositInput = "40.00";
    private string _discountCodeInput = "EARLY20";

    private Guid _selectedTemplateId;
    private string _instanceName = "Fall Leadership Conference 2026";
    private string _instanceSlug = "fall-conference-2026";
    private string _maxAttendeesInput = "50";
    private Guid _selectedInstanceId;

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var accRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true));
        if (accRes.IsSuccess)
        {
            _accounts = [.. accRes.Value];
            if (_accountId == Guid.Empty && _accounts.Count > 0)
            {
                _accountId = _accounts[0].Id;
            }
        }

        var gwRes = await QueryDispatcher.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery(ActiveOnly: true));
        if (gwRes.IsSuccess && gwRes.Value.Count > 0)
        {
            _defaultGatewayId = gwRes.Value[0].Id;
        }

        var tmplRes = await QueryDispatcher.QueryAsync<
            GetRegistrationTemplatesQuery,
            IReadOnlyList<RegistrationTemplateResponse>
        >(new GetRegistrationTemplatesQuery());
        if (tmplRes.IsSuccess)
        {
            _templates = [.. tmplRes.Value];
            if (_selectedTemplateId == Guid.Empty && _templates.Count > 0)
            {
                _selectedTemplateId = _templates[0].Id;
            }
        }

        var instRes = await QueryDispatcher.QueryAsync<
            GetRegistrationInstancesQuery,
            IReadOnlyList<RegistrationInstanceResponse>
        >(new GetRegistrationInstancesQuery());
        if (instRes.IsSuccess)
        {
            _instances = [.. instRes.Value];
        }
    }

    private async Task CreateTemplateAsync()
    {
        if (
            !decimal.TryParse(
                _baseCostInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var baseCost
            )
            || !decimal.TryParse(
                _depositInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var deposit
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter valid numeric base cost and deposit values.";
            return;
        }

        var spec = RegistrationTemplateSpecFactory.BuildDefaultSpec(
            _templateName,
            _accountId,
            _defaultGatewayId,
            baseCost,
            deposit,
            _discountCodeInput
        );
        var res = await CommandDispatcher.SendAsync<
            CreateRegistrationTemplateCommand,
            RegistrationTemplateResponse
        >(new CreateRegistrationTemplateCommand(spec));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Created registration template {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task CreateInstanceAsync()
    {
        int? maxSeats = int.TryParse(
            _maxAttendeesInput,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;

        var res = await CommandDispatcher.SendAsync<
            CreateRegistrationInstanceCommand,
            RegistrationInstanceResponse
        >(
            new CreateRegistrationInstanceCommand(
                new RegistrationInstanceSpec(
                    _selectedTemplateId,
                    _instanceName,
                    _instanceSlug,
                    MaxAttendees: maxSeats
                )
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Launched registration instance {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task LoadSubmissionsAsync(Guid instanceId)
    {
        _selectedInstanceId = instanceId;
        var res = await QueryDispatcher.QueryAsync<
            GetRegistrationsByInstanceQuery,
            IReadOnlyList<RegistrationResponse>
        >(new GetRegistrationsByInstanceQuery(instanceId));
        if (res.IsSuccess)
        {
            _registrations = [.. res.Value];
        }
    }

    private async Task CancelRegistrantAsync(Guid registrationId, Guid registrantId)
    {
        var res = await CommandDispatcher.SendAsync<
            CancelRegistrantCommand,
            CancelRegistrantResultResponse
        >(new CancelRegistrantCommand(registrationId, registrantId));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Cancelled registrant; auto-promoted {res.Value.AutoPromotedRegistrantIds.Count} waitlisted attendee(s)."
        );
        await ReloadAsync();
        await LoadSubmissionsAsync(_selectedInstanceId);
    }
}
