using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Command to create a StreakType.</summary>
/// <param name="Spec">StreakType specification.</param>
public sealed record CreateStreakTypeCommand(StreakTypeSpec Spec) : ICommand<StreakTypeResponse>;
