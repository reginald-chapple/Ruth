using Ruth.Modules.Lms.Contracts.Classes;

namespace Ruth.Modules.Lms.Contracts.Portal;

/// <summary>Learner's enrolled class workspace showing progress, activities, and completions.</summary>
/// <param name="Participant">Enrollment and grade summary.</param>
/// <param name="ClassDetail">Class content pages, announcements, and activities.</param>
/// <param name="Completions">Learner activity completion records.</param>
public sealed record LearnerClassWorkspaceResponse(
    LearningParticipantResponse Participant,
    LearningClassResponse ClassDetail,
    IReadOnlyList<LearningActivityCompletionResponse> Completions
);
