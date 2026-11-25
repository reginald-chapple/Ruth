namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Status of an assessment assigned to or taken by a person.</summary>
public enum AssessmentStatus
{
    /// <summary>Requested or in progress, not yet completed.</summary>
    Pending = 0,

    /// <summary>Assessment has been completed and scored.</summary>
    Complete = 1,
}
