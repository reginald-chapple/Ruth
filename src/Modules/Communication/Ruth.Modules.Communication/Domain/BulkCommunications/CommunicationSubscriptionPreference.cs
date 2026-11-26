using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Channel and per-topic opt-out / suppression preference aggregate for a normalized recipient destination.</summary>
public sealed class CommunicationSubscriptionPreference
    : AggregateRoot<CommunicationSubscriptionPreferenceId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    public static readonly IReadOnlyList<string> DefaultAvailableTopics =
    [
        "general-announcements",
        "events-and-programs",
        "volunteer-updates",
        "billing-and-dues",
        "community-digest",
    ];

    private HashSet<string> _optedOutTopics = new(StringComparer.OrdinalIgnoreCase);

    private CommunicationSubscriptionPreference() { }

    private CommunicationSubscriptionPreference(
        CommunicationSubscriptionPreferenceId id,
        CommunicationChannel channel,
        string normalizedDestination,
        Guid? personId
    )
        : base(id)
    {
        Channel = channel;
        NormalizedDestination = normalizedDestination;
        PersonId = personId.HasValue && personId.Value != Guid.Empty ? personId : null;
    }

    public CommunicationChannel Channel { get; private set; }
    public string NormalizedDestination { get; private set; } = string.Empty;
    public Guid? PersonId { get; private set; }
    public bool GlobalOptOut { get; private set; }
    public string? Reason { get; private set; }

    public IReadOnlyCollection<string> OptedOutTopics => _optedOutTopics;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a preference record for a validated channel destination.</summary>
    public static Result<CommunicationSubscriptionPreference> Create(
        CommunicationChannel channel,
        string? rawDestination,
        Guid? personId = null
    )
    {
        var normalized = CommunicationRecipient.NormalizeDestination(channel, rawDestination);
        return normalized.IsFailure
            ? Result.Failure<CommunicationSubscriptionPreference>(normalized.Errors)
            : new CommunicationSubscriptionPreference(
                CommunicationSubscriptionPreferenceId.Create(),
                channel,
                normalized.Value.Normalized,
                personId
            );
    }

    /// <summary>Updates global and topic-level opt-out flags.</summary>
    public void ApplyPreferences(
        bool globalOptOut,
        IEnumerable<string>? optedOutTopics,
        string? reason,
        Guid? personId = null
    )
    {
        GlobalOptOut = globalOptOut;
        _optedOutTopics.Clear();
        if (optedOutTopics is not null)
        {
            foreach (var topic in optedOutTopics)
            {
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    _ = _optedOutTopics.Add(topic.Trim().ToLowerInvariant());
                }
            }
        }

        if (personId.HasValue && personId.Value != Guid.Empty)
        {
            PersonId = personId;
        }

        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>Opts out of either a single topic or globally (* / null).</summary>
    public void ApplyOneClickOptOut(string? topicKey, string reason)
    {
        if (
            string.IsNullOrWhiteSpace(topicKey)
            || string.Equals(topicKey.Trim(), "*", StringComparison.Ordinal)
        )
        {
            GlobalOptOut = true;
        }
        else
        {
            _ = _optedOutTopics.Add(topicKey.Trim().ToLowerInvariant());
        }

        Reason = reason;
    }

    /// <summary>Determines whether a recipient should be skipped for a message with <paramref name="isBulk" /> and <paramref name="topicKey" />.</summary>
    public static (bool IsSuppressed, string? Reason) EvaluateSuppression(
        bool isBulk,
        string topicKey,
        CommunicationSubscriptionPreference? preference
    )
    {
        if (preference is null)
        {
            return (false, null);
        }

        // Hard-bounce and spam-complaint suppressions block even non-bulk sends to protect sender reputation.
        if (
            preference.GlobalOptOut
            && (
                string.Equals(preference.Reason, "HardBounce", StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    preference.Reason,
                    "SpamComplaint",
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            return (true, $"Suppressed ({preference.Reason})");
        }

        if (!isBulk)
        {
            return (false, null);
        }

        if (preference.GlobalOptOut)
        {
            return (true, "Recipient globally opted out of bulk channel messages.");
        }

        return preference._optedOutTopics.Contains(topicKey)
            ? (true, $"Recipient opted out of topic '{topicKey}'.")
            : (false, null);
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    internal void ReplaceTopicsFromPersistence(HashSet<string> topics)
    {
        _optedOutTopics = topics;
    }
}
