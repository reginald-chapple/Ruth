using Microsoft.AspNetCore.Components;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Communication;

/// <summary>Member self-service and staff-managed communication unsubscribe &amp; topic preference center.</summary>
public sealed partial class CommunicationPreferenceCenter : ComponentBase
{
    private static readonly CommunicationChannel[] _channels =
    [
        CommunicationChannel.Email,
        CommunicationChannel.Sms,
    ];

    private readonly HashSet<string> _mutedTopics = new(StringComparer.OrdinalIgnoreCase);

    private CommunicationPreferenceResponse? _preferences;
    private string _lookupDestination = "member@example.org";
    private CommunicationChannel _lookupChannel = CommunicationChannel.Email;
    private bool _globalOptOut;
    private string? _statusMessage;
    private bool _isError;

    /// <summary>Gets or sets the optional signed preference token from an email/SMS link.</summary>
    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(Token))
        {
            await LoadAsync(new GetRecipientPreferencesQuery(PreferenceToken: Token));
        }
    }

    private Task LookupPreferencesAsync()
    {
        return LoadAsync(
            new GetRecipientPreferencesQuery(
                DestinationAddress: _lookupDestination,
                Channel: _lookupChannel
            )
        );
    }

    private async Task LoadAsync(GetRecipientPreferencesQuery query)
    {
        var res = await QueryDispatcher.QueryAsync<
            GetRecipientPreferencesQuery,
            CommunicationPreferenceResponse
        >(query);

        if (res.IsSuccess)
        {
            ApplyLoaded(res.Value);
            _isError = false;
            _statusMessage = null;
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private void ToggleMutedTopic(string topic, bool muted)
    {
        if (muted)
        {
            _ = _mutedTopics.Add(topic);
            return;
        }

        _ = _mutedTopics.Remove(topic);
    }

    private async Task SavePreferencesAsync()
    {
        if (_preferences is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            UpdateRecipientPreferencesCommand,
            CommunicationPreferenceResponse
        >(
            new UpdateRecipientPreferencesCommand(
                _globalOptOut,
                [.. _mutedTopics],
                PreferenceToken: _preferences.PreferenceToken
            )
        );

        if (res.IsSuccess)
        {
            ApplyLoaded(res.Value);
            _isError = false;
            _statusMessage = "Communication preferences saved.";
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private void ApplyLoaded(CommunicationPreferenceResponse response)
    {
        _preferences = response;
        _globalOptOut = response.GlobalOptOut;
        _mutedTopics.Clear();
        foreach (var t in response.OptedOutTopics)
        {
            _ = _mutedTopics.Add(t);
        }
    }
}
