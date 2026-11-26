namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Scheduling and enrollment mode of a LearningProgram.</summary>
public enum LearningProgramConfigurationMode
{
    /// <summary>Self-paced rolling enrollment without fixed terms.</summary>
    OnDemandLearning = 0,

    /// <summary>Structured cohort schedule organized by LearningTerms.</summary>
    AcademicCalendar = 1,
}
