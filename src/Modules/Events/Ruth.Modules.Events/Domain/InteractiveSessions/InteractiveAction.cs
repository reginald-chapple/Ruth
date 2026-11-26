using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Poll, Q&amp;A prompt, word-cloud, or stage slide within an <see cref="InteractiveSession" />.</summary>
public sealed class InteractiveAction : Entity<InteractiveActionId>
{
    private List<string> _options = [];

    private InteractiveAction() { }

    private InteractiveAction(InteractiveActionId id, InteractiveSessionId sessionId)
        : base(id)
    {
        SessionId = sessionId;
    }

    public InteractiveSessionId SessionId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string PromptText { get; private set; } = string.Empty;
    public InteractiveActionType ActionType { get; private set; }
    public IReadOnlyList<string> Options
    {
        get => _options;
        private set => _options = [.. value];
    }

    public int DisplayOrder { get; private set; }
    public bool AllowMultipleResponses { get; private set; }
    public bool RequireModeration { get; private set; }
    public bool ShowLiveResultsOnStage { get; private set; } = true;
    public bool IsOpen { get; private set; } = true;

    internal static Result<InteractiveAction> Create(
        InteractiveSessionId sessionId,
        InteractiveActionSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);

        var cleanedOptions = spec.Options?.Where(static o => !string.IsNullOrWhiteSpace(o))
            .Select(static o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];

        var invalid =
            string.IsNullOrWhiteSpace(spec.Title)
            || spec.Title.Trim().Length > 160
            || string.IsNullOrWhiteSpace(spec.PromptText)
            || spec.PromptText.Trim().Length > 1000
            || (
                spec.ActionType == InteractiveActionType.MultipleChoicePoll
                && cleanedOptions.Count < 2
            );

        return invalid
            ? EventsErrors.InteractiveActionInvalid
            : new InteractiveAction(InteractiveActionId.Create(), sessionId)
            {
                Title = spec.Title.Trim(),
                PromptText = spec.PromptText.Trim(),
                ActionType = spec.ActionType,
                Options = cleanedOptions,
                DisplayOrder = spec.DisplayOrder,
                AllowMultipleResponses = spec.AllowMultipleResponses,
                RequireModeration = spec.RequireModeration,
                ShowLiveResultsOnStage = spec.ShowLiveResultsOnStage,
                IsOpen = spec.IsOpen,
            };
    }

    internal string? MatchCanonicalOption(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var trimmed = candidate.Trim();
        return _options.Find(o => string.Equals(o, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
