using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Public Opportunity Finder and constituent signup screen for W5-S1.</summary>
public sealed partial class PublicOpportunityFinder : ComponentBase
{
    private IReadOnlyList<EngagementOpportunityResponse> _opportunities = [];
    private string _searchText = string.Empty;
    private string _selectedSlug = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _comments = string.Empty;
    private string? _statusMessage;
    private bool _isError;

    [Parameter]
    public string? Slug { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Slug))
        {
            _selectedSlug = Slug;
        }

        await SearchAsync();
    }

    private void SelectOpportunity(string slug)
    {
        _selectedSlug = slug;
    }

    private async Task SearchAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetEngagementOpportunitiesQuery,
            IReadOnlyList<EngagementOpportunityResponse>
        >(
            new GetEngagementOpportunitiesQuery(
                ActiveOnly: true,
                PublicOnly: true,
                SearchText: _searchText
            )
        );
        _opportunities = res.IsSuccess ? res.Value : [];
        if (string.IsNullOrWhiteSpace(_selectedSlug) && _opportunities.Count > 0)
        {
            _selectedSlug = _opportunities[0].Slug;
        }
    }

    private async Task SubmitSignupAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            SubmitPublicEngagementSignupCommand,
            EngagementRequestResponse
        >(
            new SubmitPublicEngagementSignupCommand(
                _selectedSlug,
                _firstName,
                _lastName,
                _email,
                string.IsNullOrWhiteSpace(_phone) ? null : _phone,
                Comments: _comments
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Thank you, {res.Value.RequesterDisplayName}! Your request for {res.Value.OpportunityName} has been received."
            : res.PrimaryError.Description;
    }
}
