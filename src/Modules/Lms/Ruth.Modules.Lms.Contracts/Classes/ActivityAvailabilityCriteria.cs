namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Criteria governing when a LearningActivity becomes available to a learner.</summary>
public enum ActivityAvailabilityCriteria
{
    /// <summary>Immediately available upon enrollment.</summary>
    AlwaysAvailable = 0,

    /// <summary>Available on a fixed UTC date.</summary>
    SpecificDate = 1,

    /// <summary>Available offset days from the class term start date.</summary>
    ClassStartOffset = 2,

    /// <summary>Available offset days from learner enrollment.</summary>
    EnrollmentOffset = 3,

    /// <summary>Unlocks after the preceding activity is completed.</summary>
    AfterPreviousCompleted = 4,
}
