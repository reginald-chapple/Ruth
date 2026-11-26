using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Cms;

/// <summary>Staff W6-S3 studio managing buffered Interactions, PersonalizationSegments, and AdaptiveMessages.</summary>
public sealed partial class CmsPersonalizationAndInteractions : ComponentBase
{
    private IReadOnlyList<InteractionResponse> _interactions = [];
    private IReadOnlyList<PersonalizationSegmentResponse> _segments = [];
    private IReadOnlyList<AdaptiveMessageResponse> _messages = [];

    private string _interactionChannelKey = "member-portal";
    private string _interactionComponentKey = "home-page";
    private string _segmentName = "Active Portal Visitors";
    private string _segmentKey = "active-portal-visitors";
    private string _messageName = "Hero Member Callout";
    private string _messageKey = "hero-member-callout";
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
        var intRes = await QueryDispatcher.QueryAsync<
            GetInteractionsQuery,
            IReadOnlyList<InteractionResponse>
        >(new GetInteractionsQuery(MaxCount: 25));
        _interactions = intRes.IsSuccess ? intRes.Value : [];

        var segRes = await QueryDispatcher.QueryAsync<
            GetPersonalizationSegmentsQuery,
            IReadOnlyList<PersonalizationSegmentResponse>
        >(new GetPersonalizationSegmentsQuery());
        _segments = segRes.IsSuccess ? segRes.Value : [];

        var msgRes = await QueryDispatcher.QueryAsync<
            GetAdaptiveMessagesQuery,
            IReadOnlyList<AdaptiveMessageResponse>
        >(new GetAdaptiveMessagesQuery());
        _messages = msgRes.IsSuccess ? msgRes.Value : [];
    }

    private async Task IngestAndFlushAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            IngestInteractionsCommand,
            InteractionBufferStatusResponse
        >(
            new IngestInteractionsCommand(
                [
                    new TrackInteractionItemSpec(
                        _interactionChannelKey,
                        _interactionComponentKey,
                        "Portal Home",
                        Operation: "View",
                        Summary: "Staff test interaction"
                    ),
                ],
                FlushImmediately: true
            )
        );
        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"Flushed interaction (total flushed: {res.Value.TotalFlushedCount})."
                )
                : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private async Task CreateSegmentAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreatePersonalizationSegmentCommand,
            PersonalizationSegmentResponse
        >(
            new CreatePersonalizationSegmentCommand(
                _segmentName,
                _segmentKey,
                FilterSpec: new PersonalizationSegmentFilterSpec(
                    InteractionChannelKeys: [_interactionChannelKey],
                    MinimumInteractionCount: 1,
                    InteractionLookbackDays: 30
                )
            )
        );
        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"Created & evaluated segment '{res.Value.SegmentKey}' ({res.Value.CachedMemberCount} members)."
                )
                : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private async Task CreateAdaptiveMessageAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateAdaptiveMessageCommand,
            AdaptiveMessageResponse
        >(new CreateAdaptiveMessageCommand(_messageName, _messageKey));
        if (res.IsSuccess)
        {
            _ = await CommandDispatcher.SendAsync<
                AddAdaptiveMessageAdaptationCommand,
                AdaptiveMessageResponse
            >(
                new AddAdaptiveMessageAdaptationCommand(
                    res.Value.Id,
                    "Default Welcome",
                    "Welcome Back to the Community",
                    "Explore upcoming workshops, resources, and member updates.",
                    Order: 10,
                    CallToActionText: "Explore Events",
                    CallToActionUrl: "/events/calendars"
                )
            );
        }

        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? $"Created AdaptiveMessage '{res.Value.Key}'."
                : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private void SetStatus(bool ok, string message)
    {
        _isError = !ok;
        _statusMessage = message;
    }
}
