using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Public Support Request Intake and Moderated Community Care Wall for W5-S3.</summary>
public sealed partial class PublicSupportWallAndIntake : ComponentBase
{
    private static readonly SupportVisibilityLevel[] _visibilities =
    [
        SupportVisibilityLevel.PrivateCareTeamOnly,
        SupportVisibilityLevel.PublicCommunityWall,
    ];

    private IReadOnlyList<SupportCategoryResponse> _categories = [];
    private IReadOnlyList<SupportRequestResponse> _wall = [];
    private Guid _selectedCategoryId;
    private string _firstName = string.Empty;
    private string _email = string.Empty;
    private string _requestText = string.Empty;
    private SupportVisibilityLevel _visibility = SupportVisibilityLevel.PrivateCareTeamOnly;
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
        var catRes = await QueryDispatcher.QueryAsync<
            GetSupportCategoriesQuery,
            IReadOnlyList<SupportCategoryResponse>
        >(new GetSupportCategoriesQuery(ActiveOnly: true));
        _categories = catRes.IsSuccess ? catRes.Value : [];
        if (_selectedCategoryId == Guid.Empty && _categories.Count > 0)
        {
            _selectedCategoryId = _categories[0].Id;
        }

        var wallRes = await QueryDispatcher.QueryAsync<
            GetPublicSupportWallQuery,
            IReadOnlyList<SupportRequestResponse>
        >(new GetPublicSupportWallQuery());
        _wall = wallRes.IsSuccess ? wallRes.Value : [];
    }

    private async Task SubmitAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            SubmitSupportRequestCommand,
            SupportRequestResponse
        >(
            new SubmitSupportRequestCommand(
                _selectedCategoryId,
                string.IsNullOrWhiteSpace(_firstName) ? "Anonymous" : _firstName,
                _requestText,
                Email: string.IsNullOrWhiteSpace(_email) ? null : _email,
                VisibilityLevel: _visibility
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? "Your request has been received by our care team."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            _requestText = string.Empty;
        }

        await ReloadAsync();
    }

    private async Task SupportAsync(Guid id)
    {
        _ = await CommandDispatcher.SendAsync<
            RecordCommunitySupportCommand,
            SupportRequestResponse
        >(new RecordCommunitySupportCommand(id, IsCareTeamMember: false));
        await ReloadAsync();
    }

    private async Task FlagAsync(Guid id)
    {
        _ = await CommandDispatcher.SendAsync<FlagSupportRequestCommand, SupportRequestResponse>(
            new FlagSupportRequestCommand(id)
        );
        _isError = false;
        _statusMessage = "Flagged for care-team moderation review.";
        await ReloadAsync();
    }
}
