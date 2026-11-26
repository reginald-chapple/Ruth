using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Organization and corporate sponsor CRM directory facade page.</summary>
public sealed partial class OrganizationList : ComponentBase
{
    private List<OrganizationSummaryResponse> _organizations = [];
    private string _searchTerm = string.Empty;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _taxId = string.Empty;
    private string _sponsorTier = string.Empty;
    private string _websiteUrl = string.Empty;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await SearchAsync();
    }

    private async Task SearchAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetOrganizationsQuery,
            IReadOnlyList<OrganizationSummaryResponse>
        >(
            new GetOrganizationsQuery(
                string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm.Trim()
            )
        );
        if (res.IsSuccess)
        {
            _organizations = [.. res.Value];
        }
    }

    private async Task CreateOrganizationAsync()
    {
        var result = await CommandDispatcher.SendAsync<
            CreateOrganizationCommand,
            OrganizationDetailResponse
        >(
            new CreateOrganizationCommand(
                _name,
                _email,
                _phone,
                TaxIdentifier: _taxId,
                SponsorTier: _sponsorTier,
                WebsiteUrl: _websiteUrl
            )
        );
        if (result.IsFailure)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _name = string.Empty;
        _isError = false;
        _statusMessage = $"Registered organization {result.Value.Name}.";
        await SearchAsync();
    }
}
