namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Keyword matching modes for SmsAction rules inside an SmsPipeline.</summary>
public enum SmsKeywordMatchMode
{
    /// <summary>Trimmed inbound message matches the keyword exactly (case-insensitive).</summary>
    ExactKeyword = 0,

    /// <summary>First whitespace-delimited token matches the keyword (e.g., "RSVP YES").</summary>
    StartsWithKeyword = 1,

    /// <summary>Inbound message contains the keyword anywhere (case-insensitive).</summary>
    ContainsKeyword = 2,

    /// <summary>Matches any inbound message as a fallback catch-all action.</summary>
    FallbackAny = 3,
}
