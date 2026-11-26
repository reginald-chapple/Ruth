using System.Buffers;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Evaluates CTIA standard opt-out/opt-in keywords and active <see cref="SmsPipeline" /> action chains.</summary>
public static class SmsKeywordMatcher
{
    private static readonly SearchValues<string> _mandatoryOptOutWords = SearchValues.Create(
        ["STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT"],
        StringComparison.OrdinalIgnoreCase
    );

    private static readonly SearchValues<string> _mandatoryOptInWords = SearchValues.Create(
        ["START", "UNSTOP", "YES"],
        StringComparison.OrdinalIgnoreCase
    );

    /// <summary>Returns true when the trimmed inbound text is a standard single-word SMS opt-out command.</summary>
    public static bool IsMandatoryOptOutKeyword(string inboundBody)
    {
        if (string.IsNullOrWhiteSpace(inboundBody))
        {
            return false;
        }

        var trimmed = inboundBody.Trim();
        return !trimmed.Contains(' ', StringComparison.Ordinal)
            && _mandatoryOptOutWords.Contains(trimmed);
    }

    /// <summary>Returns true when the trimmed inbound text is a standard single-word SMS opt-in command.</summary>
    public static bool IsMandatoryOptInKeyword(string inboundBody)
    {
        if (string.IsNullOrWhiteSpace(inboundBody))
        {
            return false;
        }

        var trimmed = inboundBody.Trim();
        return !trimmed.Contains(' ', StringComparison.Ordinal)
            && _mandatoryOptInWords.Contains(trimmed);
    }

    /// <summary>Selects ordered matching <see cref="SmsAction" /> rules across active pipelines applicable to <paramref name="systemPhoneNumberId" />.</summary>
    public static IReadOnlyList<SmsAction> MatchActions(
        Guid systemPhoneNumberId,
        string inboundBody,
        IReadOnlyList<SmsPipeline> pipelines
    )
    {
        ArgumentNullException.ThrowIfNull(pipelines);
        if (string.IsNullOrWhiteSpace(inboundBody))
        {
            return [];
        }

        var matched = new List<SmsAction>();
        foreach (var pipeline in SelectOrderedPipelines(systemPhoneNumberId, pipelines))
        {
            if (AppendMatchesShouldStop(pipeline, inboundBody, matched))
            {
                return matched;
            }
        }

        return matched;
    }

    private static IEnumerable<SmsPipeline> SelectOrderedPipelines(
        Guid systemPhoneNumberId,
        IReadOnlyList<SmsPipeline> pipelines
    )
    {
        return pipelines
            .Where(p =>
                p.IsActive
                && (!p.SystemPhoneNumberId.HasValue || p.SystemPhoneNumberId == systemPhoneNumberId)
            )
            .OrderBy(static p => p.SystemPhoneNumberId.HasValue ? 0 : 1)
            .ThenBy(static p => p.PriorityOrder);
    }

    private static bool AppendMatchesShouldStop(
        SmsPipeline pipeline,
        string inboundBody,
        List<SmsAction> matched
    )
    {
        foreach (var action in pipeline.Actions.OrderBy(static a => a.Order))
        {
            if (!action.IsMatch(inboundBody))
            {
                continue;
            }

            matched.Add(action);
            if (!action.ContinueProcessing)
            {
                return true;
            }
        }

        return false;
    }
}
