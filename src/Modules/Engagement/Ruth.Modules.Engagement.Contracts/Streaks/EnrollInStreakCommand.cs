using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Streaks;

/// <summary>Command to enroll a constituent in a StreakType.</summary>
/// <param name="StreakTypeId">Target StreakType identifier.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="EnrollmentDate">Optional enrollment date.</param>
public sealed record EnrollInStreakCommand(
    Guid StreakTypeId,
    Guid PersonId,
    Guid? BranchId = null,
    DateOnly? EnrollmentDate = null
) : ICommand<StreakResponse>;
