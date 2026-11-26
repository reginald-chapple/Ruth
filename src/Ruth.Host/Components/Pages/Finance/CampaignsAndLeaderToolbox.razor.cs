using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff and group-leader workspace for managing peer-to-peer and project fundraising campaigns.</summary>
public sealed partial class CampaignsAndLeaderToolbox : ComponentBase
{
    private List<FinancialAccountResponse> _accounts = [];
    private List<FundraisingCampaignResponse> _campaigns = [];
    private CampaignLeaderToolboxResponse? _toolbox;

    private string _campaignName = "Clean Water Community Project";
    private string _campaignSlug = "clean-water-2026";
    private string _groupIdInput = Guid.CreateVersion7().ToString("D");
    private Guid _accountId;
    private string _campaignGoalInput = "10000.00";
    private string _defaultParticipantGoalInput = "1000.00";

    private string _teamGoalOverrideInput = "12000.00";
    private string _participantPersonIdInput = Guid.CreateVersion7().ToString("D");
    private string _participantNameInput = "Alex Morgan";
    private string _participantGoalInput = "1500.00";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
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

        var campRes = await QueryDispatcher.QueryAsync<
            GetFundraisingCampaignsQuery,
            IReadOnlyList<FundraisingCampaignResponse>
        >(new GetFundraisingCampaignsQuery());
        if (campRes.IsSuccess)
        {
            _campaigns = [.. campRes.Value];
        }
    }

    private async Task CreateCampaignAsync()
    {
        if (
            !Guid.TryParse(_groupIdInput, out var groupId)
            || !decimal.TryParse(
                _campaignGoalInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var goal
            )
            || !decimal.TryParse(
                _defaultParticipantGoalInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var partGoal
            )
        )
        {
            _isError = true;
            _statusMessage = "Provide a valid Group GUID and numeric goals.";
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateFundraisingCampaignCommand,
            FundraisingCampaignResponse
        >(
            new CreateFundraisingCampaignCommand(
                new FundraisingCampaignSpec(
                    _campaignName,
                    _campaignSlug,
                    groupId,
                    _accountId,
                    goal,
                    partGoal
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
        _statusMessage = $"Created campaign {res.Value.Name}.";
        await ReloadAsync();
        await LoadToolboxAsync(res.Value.Id);
    }

    private async Task LoadToolboxAsync(Guid campaignId)
    {
        var res = await QueryDispatcher.QueryAsync<
            GetCampaignLeaderToolboxQuery,
            CampaignLeaderToolboxResponse
        >(new GetCampaignLeaderToolboxQuery(campaignId));
        if (res.IsSuccess)
        {
            _toolbox = res.Value;
        }
    }

    private async Task ApplyTeamGoalOverrideAsync()
    {
        if (_toolbox is null)
        {
            return;
        }

        decimal? overrideVal = decimal.TryParse(
            _teamGoalOverrideInput,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;

        var res = await CommandDispatcher.SendAsync<
            SetCampaignTeamGoalOverrideCommand,
            FundraisingCampaignResponse
        >(new SetCampaignTeamGoalOverrideCommand(_toolbox.Campaign.Id, overrideVal));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = "Updated team goal override.";
        await ReloadAsync();
        await LoadToolboxAsync(_toolbox.Campaign.Id);
    }

    private async Task SyncGroupRosterAsync()
    {
        if (_toolbox is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            SyncGroupMembersToCampaignCommand,
            IReadOnlyList<CampaignParticipantResponse>
        >(new SyncGroupMembersToCampaignCommand(_toolbox.Campaign.Id));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Synced {res.Value.Count} campaign participant(s)."
        );
        await LoadToolboxAsync(_toolbox.Campaign.Id);
    }

    private async Task EnrollParticipantAsync()
    {
        if (_toolbox is null || !Guid.TryParse(_participantPersonIdInput, out var personId))
        {
            _isError = true;
            _statusMessage = "Provide a valid participant Person GUID.";
            return;
        }

        decimal? customGoal = decimal.TryParse(
            _participantGoalInput,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;

        var res = await CommandDispatcher.SendAsync<
            EnrollCampaignParticipantCommand,
            CampaignParticipantResponse
        >(
            new EnrollCampaignParticipantCommand(
                _toolbox.Campaign.Id,
                personId,
                _participantNameInput,
                CustomGoalAmount: customGoal
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Enrolled {res.Value.DisplayName}.";
        await LoadToolboxAsync(_toolbox.Campaign.Id);
    }
}
