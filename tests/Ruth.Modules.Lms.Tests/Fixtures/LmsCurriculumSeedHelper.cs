using System.Text.Json;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Lms.Tests.Fixtures;

internal static class LmsCurriculumSeedHelper
{
    public sealed record SeededCurriculum(
        Guid Course1Id,
        Guid Course2Id,
        Guid Class1Id,
        Guid Class2Id,
        Guid QuizId,
        Guid UploadId
    );

    public static async Task<SeededCurriculum> SeedAsync(ICommandDispatcher commands)
    {
        var grading = await commands.SendAsync<
            CreateLearningGradingSystemCommand,
            LearningGradingSystemResponse
        >(
            new CreateLearningGradingSystemCommand(
                "Certification Scale",
                [
                    new LearningGradingScaleSpec("Pass", 80m, IsPassing: true),
                    new LearningGradingScaleSpec("Fail", 0m, IsPassing: false),
                ]
            )
        );
        Assert.True(grading.IsSuccess);

        var program = await commands.SendAsync<
            CreateLearningProgramCommand,
            LearningProgramResponse
        >(
            new CreateLearningProgramCommand(
                "Volunteer Academy",
                "Volunteer Certification",
                LearningProgramConfigurationMode.AcademicCalendar,
                Summary: "Foundational training track",
                DefaultGradingSystemId: grading.Value.Id
            )
        );
        Assert.True(program.IsSuccess);

        var term = await commands.SendAsync<CreateLearningTermCommand, LearningTermResponse>(
            new CreateLearningTermCommand(
                program.Value.Id,
                "Fall Cohort",
                StartDate: new DateOnly(2026, 9, 1),
                EndDate: new DateOnly(2026, 12, 1)
            )
        );
        Assert.True(term.IsSuccess);

        var (course1Id, course2Id) = await SeedCoursesAsync(commands, program.Value.Id);
        var (class1Id, class2Id, quizId, uploadId) = await SeedClassesAndActivitiesAsync(
            commands,
            course1Id,
            course2Id,
            term.Value.Id,
            grading.Value.Id
        );
        return new SeededCurriculum(course1Id, course2Id, class1Id, class2Id, quizId, uploadId);
    }

    private static async Task<(Guid Course1Id, Guid Course2Id)> SeedCoursesAsync(
        ICommandDispatcher commands,
        Guid programId
    )
    {
        var c1 = await commands.SendAsync<CreateLearningCourseCommand, LearningCourseResponse>(
            new CreateLearningCourseCommand(programId, "Safety 101", "SAFE-101", Credits: 2)
        );
        var c2 = await commands.SendAsync<CreateLearningCourseCommand, LearningCourseResponse>(
            new CreateLearningCourseCommand(programId, "Lead Mentor 201", "SAFE-201", Credits: 3)
        );
        Assert.True(c1.IsSuccess);
        Assert.True(c2.IsSuccess);

        var req = await commands.SendAsync<AddCourseRequirementCommand, LearningCourseResponse>(
            new AddCourseRequirementCommand(
                c2.Value.Id,
                c1.Value.Id,
                CourseRequirementType.Prerequisite
            )
        );
        Assert.True(req.IsSuccess);
        return (c1.Value.Id, c2.Value.Id);
    }

    private static async Task<(
        Guid Class1Id,
        Guid Class2Id,
        Guid QuizId,
        Guid UploadId
    )> SeedClassesAndActivitiesAsync(
        ICommandDispatcher commands,
        Guid course1Id,
        Guid course2Id,
        Guid termId,
        Guid gradingId
    )
    {
        var cls1 = await commands.SendAsync<CreateLearningClassCommand, LearningClassResponse>(
            new CreateLearningClassCommand(
                course1Id,
                "Safety 101 — Fall",
                termId,
                gradingId,
                MaxStudents: 2
            )
        );
        var cls2 = await commands.SendAsync<CreateLearningClassCommand, LearningClassResponse>(
            new CreateLearningClassCommand(course2Id, "Lead Mentor — Fall", termId, gradingId)
        );
        Assert.True(cls1.IsSuccess);
        Assert.True(cls2.IsSuccess);

        var page = await commands.SendAsync<
            AddClassContentPageCommand,
            LearningClassContentPageResponse
        >(new AddClassContentPageCommand(cls1.Value.Id, "Syllabus", "<p>Welcome</p>", 1));
        var ann = await commands.SendAsync<
            AddClassAnnouncementCommand,
            LearningClassAnnouncementResponse
        >(new AddClassAnnouncementCommand(cls1.Value.Id, "Kickoff", "<p>Starts Monday</p>"));
        Assert.True(page.IsSuccess);
        Assert.True(ann.IsSuccess);

        var quiz = await commands.SendAsync<AddLearningActivityCommand, LearningActivityResponse>(
            new AddLearningActivityCommand(
                cls1.Value.Id,
                "Safety Check",
                LearningActivityComponentType.Assessment,
                Points: 50,
                Order: 1,
                ActivityConfigJson: JsonSerializer.Serialize(
                    new { answerKey = new { q1 = "Yes", q2 = "A" } }
                )
            )
        );
        var upload = await commands.SendAsync<AddLearningActivityCommand, LearningActivityResponse>(
            new AddLearningActivityCommand(
                cls1.Value.Id,
                "Signed Acknowledgment PDF",
                LearningActivityComponentType.FileUpload,
                Points: 50,
                Order: 2
            )
        );
        Assert.True(quiz.IsSuccess);
        Assert.True(upload.IsSuccess);

        return (cls1.Value.Id, cls2.Value.Id, quiz.Value.Id, upload.Value.Id);
    }
}
