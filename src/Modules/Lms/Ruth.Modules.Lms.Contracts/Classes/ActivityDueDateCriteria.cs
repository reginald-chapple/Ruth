namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Criteria governing when a LearningActivity is due.</summary>
public enum ActivityDueDateCriteria
{
    /// <summary>No due date.</summary>
    NoDate = 0,

    /// <summary>Due on a fixed UTC date.</summary>
    SpecificDate = 1,

    /// <summary>Due offset days from the class term start date.</summary>
    ClassStartOffset = 2,

    /// <summary>Due offset days from learner enrollment.</summary>
    EnrollmentOffset = 3,
}
