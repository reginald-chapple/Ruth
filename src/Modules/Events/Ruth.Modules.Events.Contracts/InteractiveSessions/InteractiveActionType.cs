namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Kind of interactive prompt or slide within an InteractiveSession.</summary>
public enum InteractiveActionType
{
    /// <summary>Multiple-choice audience poll with live option tallies.</summary>
    MultipleChoicePoll = 0,

    /// <summary>Open-text audience Q&amp;A prompt with optional moderator approval.</summary>
    OpenTextQa = 1,

    /// <summary>Short-phrase word-cloud / pulse prompt.</summary>
    WordCloudPrompt = 2,

    /// <summary>Stage announcement or speaker callout slide.</summary>
    AnnouncementSlide = 3,
}
