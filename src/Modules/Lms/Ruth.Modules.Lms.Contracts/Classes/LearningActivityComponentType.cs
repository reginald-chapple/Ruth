namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Activity component kind supported by the LMS.</summary>
public enum LearningActivityComponentType
{
    /// <summary>Video lesson with playback completion.</summary>
    Video = 0,

    /// <summary>Written article or reading module.</summary>
    Article = 1,

    /// <summary>Learner file submission reviewed by a facilitator.</summary>
    FileUpload = 2,

    /// <summary>Graded quiz or structured assessment.</summary>
    Assessment = 3,

    /// <summary>Policy, handbook, or safety acknowledgment sign-off.</summary>
    Acknowledgment = 4,
}
