using Ruth.Modules.Engagement.Contracts.Touchpoints;

namespace Ruth.Modules.Engagement.Domain.Touchpoints;

/// <summary>Clean-room cadence due-date and relationship health calculator for ConstituentContacts.</summary>
public static class TouchpointCadenceScheduler
{
    /// <summary>Computes the next outreach due date from the most recent touchpoint or creation date.</summary>
    public static DateOnly? ComputeNextOutreachDate(
        int outreachCadenceDays,
        DateTimeOffset? lastTouchpointAtUtc,
        DateOnly baselineDate
    )
    {
        if (outreachCadenceDays <= 0)
        {
            return null;
        }

        var anchor = lastTouchpointAtUtc.HasValue
            ? DateOnly.FromDateTime(lastTouchpointAtUtc.Value.UtcDateTime)
            : baselineDate;
        return anchor.AddDays(outreachCadenceDays);
    }

    /// <summary>Computes the next personal connection-note due date when configured.</summary>
    public static DateOnly? ComputeNextConnectionNoteDate(
        int? connectionNoteCadenceDays,
        DateTimeOffset? lastNoteAtUtc,
        DateOnly baselineDate
    )
    {
        if (connectionNoteCadenceDays is not > 0)
        {
            return null;
        }

        var anchor = lastNoteAtUtc.HasValue
            ? DateOnly.FromDateTime(lastNoteAtUtc.Value.UtcDateTime)
            : baselineDate;
        return anchor.AddDays(connectionNoteCadenceDays.Value);
    }

    /// <summary>Evaluates first-principles relationship health score (0..100) and status.</summary>
    public static (int Score, ConstituentHealthStatus Status) EvaluateHealth(
        bool isActive,
        ContactRelationshipStrength strength,
        int outreachCadenceDays,
        DateOnly? nextScheduledDate,
        TouchpointOutcomeSentiment? latestSentiment,
        DateOnly today
    )
    {
        if (!isActive)
        {
            return (0, ConstituentHealthStatus.Paused);
        }

        var rawBase = GetBaseScore(strength) + GetSentimentDelta(latestSentiment);
        if (outreachCadenceDays <= 0 || !nextScheduledDate.HasValue)
        {
            return (Math.Clamp(rawBase, 0, 100), ConstituentHealthStatus.Healthy);
        }

        var overdueDays = today.DayNumber - nextScheduledDate.Value.DayNumber;
        if (overdueDays <= -3)
        {
            return (Math.Clamp(rawBase, 0, 100), ConstituentHealthStatus.Healthy);
        }

        if (overdueDays <= 0)
        {
            return (Math.Clamp(rawBase - 5, 0, 100), ConstituentHealthStatus.DueSoon);
        }

        if (overdueDays <= outreachCadenceDays)
        {
            var penalty =
                10
                + (int)
                    Math.Round(
                        20.0 * overdueDays / outreachCadenceDays,
                        MidpointRounding.AwayFromZero
                    );
            return (Math.Clamp(rawBase - penalty, 0, 100), ConstituentHealthStatus.Overdue);
        }

        return (Math.Clamp(rawBase - 45, 0, 100), ConstituentHealthStatus.CriticalLapse);
    }

    private static int GetBaseScore(ContactRelationshipStrength strength)
    {
        return strength switch
        {
            ContactRelationshipStrength.Strong => 90,
            ContactRelationshipStrength.Established => 80,
            ContactRelationshipStrength.Developing => 70,
            ContactRelationshipStrength.New => 60,
            ContactRelationshipStrength.AtRisk => 40,
            _ => 65,
        };
    }

    private static int GetSentimentDelta(TouchpointOutcomeSentiment? sentiment)
    {
        return sentiment switch
        {
            TouchpointOutcomeSentiment.Positive => 10,
            TouchpointOutcomeSentiment.Neutral => 0,
            TouchpointOutcomeSentiment.NeedsFollowUp => -10,
            TouchpointOutcomeSentiment.NoResponse => -15,
            null => 0,
            _ => 0,
        };
    }
}
