namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Role of a participant enrolled in a LearningClass.</summary>
public enum LearningParticipantRole
{
    /// <summary>Learner completing course activities.</summary>
    Student = 0,

    /// <summary>Instructor or facilitator grading submissions.</summary>
    Facilitator = 1,
}
