namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Overall completion outcome for a LearningParticipant in a class.</summary>
public enum LearningCompletionStatus
{
    /// <summary>Course is still in progress.</summary>
    Incomplete = 0,

    /// <summary>Learner met all passing grade criteria.</summary>
    Pass = 1,

    /// <summary>Learner completed course below passing threshold.</summary>
    Fail = 2,
}
