namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Persisted LearningClass with content pages, announcements, and activities.</summary>
/// <param name="Id">Class identifier.</param>
/// <param name="LearningCourseId">Parent course identifier.</param>
/// <param name="LearningTermId">Optional term identifier.</param>
/// <param name="LearningGradingSystemId">Optional grading system identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="VirtualRoomId">Optional embedded LiveKit VirtualRoom identifier.</param>
/// <param name="Name">Class name.</param>
/// <param name="IsPublic">Whether publicly enrollable.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="TakesAttendance">Whether attendance is tracked.</param>
/// <param name="AllowOverbooking">Whether enrollment may exceed MaxStudents.</param>
/// <param name="MaxStudents">Optional enrollment cap.</param>
/// <param name="EnrolledStudentCount">Current student headcount.</param>
/// <param name="ContentPages">Ordered content pages.</param>
/// <param name="Announcements">Announcements.</param>
/// <param name="Activities">Ordered learning activities.</param>
public sealed record LearningClassResponse(
    Guid Id,
    Guid LearningCourseId,
    Guid? LearningTermId,
    Guid? LearningGradingSystemId,
    Guid? BranchId,
    Guid? VirtualRoomId,
    string Name,
    bool IsPublic,
    bool IsActive,
    bool TakesAttendance,
    bool AllowOverbooking,
    int? MaxStudents,
    int EnrolledStudentCount,
    IReadOnlyList<LearningClassContentPageResponse> ContentPages,
    IReadOnlyList<LearningClassAnnouncementResponse> Announcements,
    IReadOnlyList<LearningActivityResponse> Activities
);
