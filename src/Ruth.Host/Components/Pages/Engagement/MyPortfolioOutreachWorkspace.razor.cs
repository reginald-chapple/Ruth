using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Mobile and Web My Portfolio outreach workspace and relationship cadence screen for W5-S4.</summary>
public sealed partial class MyPortfolioOutreachWorkspace : ComponentBase
{
    private IReadOnlyList<PersonSummaryResponse> _people = [];
    private PortfolioOutreachQueueResponse? _portfolio;
    private Guid _ownerPersonId;
    private Guid _constituentPersonId;
    private string _portfolioRole = "Steward";
    private string _cadenceDaysInput = "30";
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
        var pplRes = await QueryDispatcher.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery(MaxResults: 50));
        _people = pplRes.IsSuccess ? pplRes.Value : [];
        if (_people.Count >= 2 && _ownerPersonId == Guid.Empty)
        {
            _ownerPersonId = _people[0].Id;
            _constituentPersonId = _people[1].Id;
        }

        var portRes = await QueryDispatcher.QueryAsync<
            GetMyPortfolioOutreachQueueQuery,
            PortfolioOutreachQueueResponse
        >(new GetMyPortfolioOutreachQueueQuery());
        _portfolio = portRes.IsSuccess ? portRes.Value : null;
    }

    private async Task AddContactAsync()
    {
        if (!int.TryParse(_cadenceDaysInput, CultureInfo.InvariantCulture, out var cadence))
        {
            cadence = 30;
        }

        var res = await CommandDispatcher.SendAsync<
            UpsertConstituentContactCommand,
            ConstituentContactResponse
        >(
            new UpsertConstituentContactCommand(
                new ConstituentContactSpec(
                    _ownerPersonId,
                    _constituentPersonId,
                    _portfolioRole,
                    OutreachCadenceDays: cadence
                )
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Saved portfolio contact {res.Value.ConstituentDisplayName}."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task CompleteAsync(Guid touchpointId, TouchpointChannel channel)
    {
        var res = await CommandDispatcher.SendAsync<CompleteTouchpointCommand, TouchpointResponse>(
            new CompleteTouchpointCommand(
                touchpointId,
                channel,
                TouchpointOutcomeSentiment.Positive,
                "Meaningful check-in completed via Portfolio Workspace",
                ContactRelationshipStrength.Strong
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? "Touchpoint completed & CRM relationship health synced."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task SnoozeWeekAsync(Guid touchpointId, DateOnly current)
    {
        _ = await CommandDispatcher.SendAsync<SnoozeTouchpointCommand, TouchpointResponse>(
            new SnoozeTouchpointCommand(touchpointId, current.AddDays(7), "Snoozed 7 days")
        );
        await ReloadAsync();
    }

    private async Task SkipAsync(Guid touchpointId)
    {
        _ = await CommandDispatcher.SendAsync<SkipTouchpointCommand, TouchpointResponse>(
            new SkipTouchpointCommand(touchpointId, "Skipped current cycle")
        );
        await ReloadAsync();
    }

    private async Task GenerateCadenceAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            GeneratePortfolioCadenceTouchpointsCommand,
            TouchpointCadenceRunResponse
        >(new GeneratePortfolioCadenceTouchpointsCommand());
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Generated {res.Value.GeneratedTouchpointCount} touchpoints across {res.Value.RefreshedContactCount} contacts."
            )
            : res.PrimaryError.Description;
        await ReloadAsync();
    }
}
