namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Role responsible for completing an LMS activity.</summary>
public enum LearningActivityAssignTo
{
    /// <summary>Assigned to enrolled learners.</summary>
    Student = 0,

    /// <summary>Assigned to class facilitators for evaluation.</summary>
    Facilitator = 1,
}
