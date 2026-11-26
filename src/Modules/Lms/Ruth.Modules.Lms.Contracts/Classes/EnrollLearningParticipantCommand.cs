using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Enrolls a Person as a Student or Facilitator in a LearningClass, enforcing prerequisites and capacity.</summary>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="PersonId">Person identifier.</param>
/// <param name="Role">Student or Facilitator.</param>
/// <param name="BypassPrerequisites">Optional staff override for prerequisites.</param>
public sealed record EnrollLearningParticipantCommand(
    Guid LearningClassId,
    Guid PersonId,
    LearningParticipantRole Role = LearningParticipantRole.Student,
    bool BypassPrerequisites = false
) : ICommand<LearningParticipantResponse>;
