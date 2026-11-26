using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Command to update a StreakType.</summary>
/// <param name="StreakTypeId">StreakType identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdateStreakTypeCommand(Guid StreakTypeId, StreakTypeSpec Spec)
    : ICommand<StreakTypeResponse>;
