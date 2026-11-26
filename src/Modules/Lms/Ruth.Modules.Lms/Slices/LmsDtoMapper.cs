using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.Modules.Lms.Domain.Programs;

namespace Ruth.Modules.Lms.Slices;

internal static class LmsDtoMapper
{
    public static LearningGradingSystemResponse ToResponse(LearningGradingSystem system)
    {
        List<LearningGradingScaleResponse> scales =
        [
            .. system
                .Scales.OrderBy(static s => s.Order)
                .Select(static s => new LearningGradingScaleResponse(
                    s.Id.Value,
                    s.Name,
                    s.ThresholdPercentage,
                    s.IsPassing,
                    s.HighlightColor,
                    s.Description
                )),
        ];
        return new LearningGradingSystemResponse(
            system.Id.Value,
            system.Name,
            system.Description,
            system.IsActive,
            scales
        );
    }

    public static LearningProgramResponse ToResponse(LearningProgram p)
    {
        return new LearningProgramResponse(
            p.Id.Value,
            p.Name,
            p.PublicName,
            p.Summary,
            p.Description,
            p.ConfigurationMode,
            p.CategoryId,
            p.DefaultGradingSystemId?.Value,
            p.HighlightColor,
            p.IconCssClass,
            p.IsPublic,
            p.IsActive,
            p.IsCompletionStatusTracked
        );
    }

    public static LearningTermResponse ToResponse(LearningTerm t)
    {
        return new LearningTermResponse(
            t.Id.Value,
            t.LearningProgramId.Value,
            t.Name,
            t.PublicName,
            t.StartDate,
            t.EndDate,
            t.EnrollmentCloseDate,
            t.IsActive
        );
    }

    public static LearningCourseResponse ToResponse(
        LearningCourse c,
        IReadOnlyDictionary<LearningCourseId, string>? courseNames = null
    )
    {
        List<LearningCourseRequirementResponse> reqs =
        [
            .. c.Requirements.Select(r => new LearningCourseRequirementResponse(
                r.Id.Value,
                r.RequiredLearningCourseId.Value,
                courseNames is not null
                && courseNames.TryGetValue(r.RequiredLearningCourseId, out var name)
                    ? name
                    : r.RequiredLearningCourseId.ToString(),
                r.RequirementType
            )),
        ];
        return new LearningCourseResponse(
            c.Id.Value,
            c.LearningProgramId.Value,
            c.Name,
            c.PublicName,
            c.CourseCode,
            c.Summary,
            c.Description,
            c.Credits,
            c.MaxStudents,
            c.AllowHistoricalAccess,
            c.EnableAnnouncements,
            c.IsPublic,
            c.IsActive,
            c.Order,
            reqs
        );
    }

    public static LearningClassContentPageResponse ToResponse(LearningClassContentPage page)
    {
        return new(page.Id.Value, page.Title, page.ContentHtml, page.Order, page.StartDateTimeUtc);
    }

    public static LearningClassAnnouncementResponse ToResponse(LearningClassAnnouncement ann)
    {
        return new(
            ann.Id.Value,
            ann.Title,
            ann.Summary,
            ann.DescriptionHtml,
            ann.PublishDateTimeUtc
        );
    }

    public static LearningActivityResponse ToResponse(LearningActivity a)
    {
        return new LearningActivityResponse(
            a.Id.Value,
            a.LearningClassId.Value,
            a.Name,
            a.Description,
            a.ComponentType,
            a.AssignTo,
            a.Order,
            a.Points,
            a.AvailabilityCriteria,
            a.AvailableDateDefault,
            a.AvailableDateOffsetDays,
            a.DueDateCriteria,
            a.DueDateDefault,
            a.DueDateOffsetDays,
            a.IsStudentCommentingEnabled,
            a.ActivityConfigJson
        );
    }

    public static LearningClassResponse ToResponse(LearningClass c, int enrolledStudentCount)
    {
        return new LearningClassResponse(
            c.Id.Value,
            c.LearningCourseId.Value,
            c.LearningTermId?.Value,
            c.LearningGradingSystemId?.Value,
            c.BranchId,
            c.Name,
            c.IsPublic,
            c.IsActive,
            c.TakesAttendance,
            c.AllowOverbooking,
            c.MaxStudents,
            enrolledStudentCount,
            [.. c.ContentPages.OrderBy(static p => p.Order).Select(ToResponse)],
            [
                .. c
                    .Announcements.OrderByDescending(static a => a.PublishDateTimeUtc)
                    .Select(ToResponse),
            ],
            [.. c.Activities.OrderBy(static a => a.Order).Select(ToResponse)]
        );
    }

    public static LearningParticipantResponse ToResponse(
        LearningParticipant p,
        int completedCount,
        int totalCount
    )
    {
        return new LearningParticipantResponse(
            p.Id.Value,
            p.LearningClassId.Value,
            p.PersonId,
            p.Role,
            p.LearningCompletionStatus,
            p.LearningCompletionDateTimeUtc,
            p.CurrentGradePercent,
            p.CurrentGradeScaleName,
            p.EnrolledDateTimeUtc,
            completedCount,
            totalCount
        );
    }

    public static LearningActivityCompletionResponse ToResponse(
        LearningActivityCompletion comp,
        LearningActivity activity
    )
    {
        return new LearningActivityCompletionResponse(
            comp.Id.Value,
            comp.LearningClassId.Value,
            comp.LearningActivityId.Value,
            activity.Name,
            activity.ComponentType,
            comp.LearningParticipantId.Value,
            comp.PersonId,
            comp.IsStudentCompleted,
            comp.IsFacilitatorCompleted,
            comp.RequiresGrading,
            comp.PointsEarned,
            activity.Points,
            comp.WasCompletedOnTime,
            comp.CompletedDateTimeUtc,
            comp.StudentComment,
            comp.FacilitatorComment,
            comp.BinaryFileId,
            comp.GradedByPersonId,
            comp.GradedDateTimeUtc
        );
    }
}
