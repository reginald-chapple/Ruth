using System.Text.Json;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.Modules.Lms.Domain.Programs;
using Xunit;

namespace Ruth.Modules.Lms.Tests.Domain;

public sealed class W6S7LmsDomainTests
{
    [Fact]
    public void GradingSystem_EvaluateScale_SelectsHighestMatchingBand()
    {
        var sysRes = LearningGradingSystem.Create(
            "Standard Letter",
            [
                new LearningGradingScaleSpec("F", 0m, IsPassing: false),
                new LearningGradingScaleSpec("B", 80m, IsPassing: true),
                new LearningGradingScaleSpec("A", 90m, IsPassing: true),
                new LearningGradingScaleSpec("C", 70m, IsPassing: true),
            ]
        );
        Assert.True(sysRes.IsSuccess);
        var sys = sysRes.Value;

        Assert.Equal("A", sys.EvaluateScale(95m)?.Name);
        Assert.Equal("B", sys.EvaluateScale(82.5m)?.Name);
        Assert.Equal("C", sys.EvaluateScale(70m)?.Name);
        var fail = sys.EvaluateScale(55m);
        Assert.NotNull(fail);
        Assert.Equal("F", fail.Name);
        Assert.False(fail.IsPassing);
    }

    [Fact]
    public void GradingSystem_RejectsEmptyScalesAndInvalidThresholds()
    {
        Assert.True(LearningGradingSystem.Create("", []).IsFailure);
        Assert.True(LearningGradingSystem.Create("Empty", []).IsFailure);
        Assert.True(
            LearningGradingSystem
                .Create("Bad", [new LearningGradingScaleSpec("X", 105m, IsPassing: true)])
                .IsFailure
        );
    }

    [Fact]
    public void Program_And_Term_ValidateDatesAndModes()
    {
        var prog = LearningProgram.Create(
            "Leadership Track",
            publicName: "Community Leadership",
            LearningProgramConfigurationMode.AcademicCalendar
        );
        Assert.True(prog.IsSuccess);
        Assert.Equal("Community Leadership", prog.Value.PublicName);

        var badTerm = LearningTerm.Create(
            prog.Value.Id,
            "Invalid Term",
            startDate: new DateOnly(2026, 9, 1),
            endDate: new DateOnly(2026, 8, 1)
        );
        Assert.True(badTerm.IsFailure);

        var goodTerm = LearningTerm.Create(
            prog.Value.Id,
            "Fall 2026",
            startDate: new DateOnly(2026, 9, 1),
            endDate: new DateOnly(2026, 12, 15)
        );
        Assert.True(goodTerm.IsSuccess);
    }

    [Fact]
    public void CoursePrerequisiteEvaluator_HandlesPrereqsEquivalentsAndCycles()
    {
        var progId = LearningProgramId.Create();
        var c1 = LearningCourse.Create(progId, "Intro", "LEAD-101").Value;
        var c2 = LearningCourse.Create(progId, "Advanced", "LEAD-201").Value;
        var cEq = LearningCourse.Create(progId, "Prior Credit", "LEAD-EQ").Value;

        Assert.True(c2.AddRequirement(c2.Id, CourseRequirementType.Prerequisite).IsFailure);
        Assert.True(c2.AddRequirement(c1.Id, CourseRequirementType.Prerequisite).IsSuccess);
        Assert.True(c2.AddRequirement(cEq.Id, CourseRequirementType.Equivalent).IsSuccess);

        Assert.False(
            CoursePrerequisiteEvaluator.ArePrerequisitesMet(c2, new HashSet<LearningCourseId>())
        );
        Assert.True(
            CoursePrerequisiteEvaluator.ArePrerequisitesMet(
                c2,
                new HashSet<LearningCourseId> { c1.Id }
            )
        );
        Assert.True(
            CoursePrerequisiteEvaluator.ArePrerequisitesMet(
                c2,
                new HashSet<LearningCourseId> { cEq.Id }
            )
        );

        var graph = new Dictionary<LearningCourseId, IReadOnlyList<LearningCourseId>>
        {
            [c2.Id] = [c1.Id],
        };
        Assert.True(CoursePrerequisiteEvaluator.WouldCreateCycle(c1.Id, c2.Id, graph));
        Assert.False(CoursePrerequisiteEvaluator.WouldCreateCycle(cEq.Id, c2.Id, graph));
    }

    [Fact]
    public void Activity_CalculateDueDateUtc_ComputesAllCriteria()
    {
        var classId = LearningClassId.Create();
        var enrolled = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var termStart = new DateOnly(2026, 9, 15);
        var fixedDue = new DateTimeOffset(2026, 11, 1, 23, 59, 0, TimeSpan.Zero);

        var a1 = LearningActivity
            .Create(
                classId,
                "Enroll Offset",
                LearningActivityComponentType.Video,
                10,
                1,
                dueDateCriteria: ActivityDueDateCriteria.EnrollmentOffset,
                dueDateOffsetDays: 7
            )
            .Value;
        var a2 = LearningActivity
            .Create(
                classId,
                "Class Start Offset",
                LearningActivityComponentType.Article,
                10,
                2,
                dueDateCriteria: ActivityDueDateCriteria.ClassStartOffset,
                dueDateOffsetDays: 14
            )
            .Value;
        var a3 = LearningActivity
            .Create(
                classId,
                "Specific",
                LearningActivityComponentType.Acknowledgment,
                10,
                3,
                dueDateCriteria: ActivityDueDateCriteria.SpecificDate,
                dueDateDefault: fixedDue
            )
            .Value;

        Assert.Equal(enrolled.AddDays(7), a1.CalculateDueDateUtc(enrolled, termStart));
        Assert.Equal(
            new DateOnly(2026, 9, 29),
            DateOnly.FromDateTime(a2.CalculateDueDateUtc(enrolled, termStart)!.Value.UtcDateTime)
        );
        Assert.Equal(fixedDue, a3.CalculateDueDateUtc(enrolled, termStart));
    }

    [Fact]
    public void AssessmentAutoGrader_HandlesObjectiveSubjectiveAndFileUploads()
    {
        var classId = LearningClassId.Create();
        var video = LearningActivity
            .Create(classId, "Intro Video", LearningActivityComponentType.Video, 20, 1)
            .Value;
        var upload = LearningActivity
            .Create(classId, "Essay", LearningActivityComponentType.FileUpload, 50, 2)
            .Value;
        var quiz = LearningActivity
            .Create(
                classId,
                "Safety Quiz",
                LearningActivityComponentType.Assessment,
                100,
                3,
                activityConfigJson: JsonSerializer.Serialize(
                    new { answerKey = new { q1 = "B", q2 = "True" } }
                )
            )
            .Value;
        var subjective = LearningActivity
            .Create(classId, "Reflection", LearningActivityComponentType.Assessment, 30, 4)
            .Value;

        var vOut = AssessmentAutoGrader.EvaluateSubmission(video, submissionDataJson: null);
        Assert.False(vOut.RequiresGrading);
        Assert.Equal(20, vOut.PointsEarned);

        var uOut = AssessmentAutoGrader.EvaluateSubmission(upload, submissionDataJson: null);
        Assert.True(uOut.RequiresGrading);
        Assert.Equal(0, uOut.PointsEarned);

        var qFull = AssessmentAutoGrader.EvaluateSubmission(
            quiz,
            JsonSerializer.Serialize(new { answers = new { q1 = "b", q2 = "true" } })
        );
        Assert.False(qFull.RequiresGrading);
        Assert.Equal(100, qFull.PointsEarned);

        var qHalf = AssessmentAutoGrader.EvaluateSubmission(
            quiz,
            JsonSerializer.Serialize(new { q1 = "B", q2 = "False" })
        );
        Assert.False(qHalf.RequiresGrading);
        Assert.Equal(50, qHalf.PointsEarned);

        var sOut = AssessmentAutoGrader.EvaluateSubmission(
            subjective,
            JsonSerializer.Serialize(new { essay = "Thoughtful response" })
        );
        Assert.True(sOut.RequiresGrading);
    }

    [Fact]
    public void Participant_RecalculateProgress_TransitionsIncompleteToPassOrFail()
    {
        var sys = LearningGradingSystem
            .Create(
                "Pass/Fail",
                [
                    new LearningGradingScaleSpec("Pass", 75m, IsPassing: true),
                    new LearningGradingScaleSpec("Fail", 0m, IsPassing: false),
                ]
            )
            .Value;
        var classId = LearningClassId.Create();
        var act1 = LearningActivity
            .Create(
                classId,
                "Quiz",
                LearningActivityComponentType.Assessment,
                60,
                1,
                activityConfigJson: JsonSerializer.Serialize(new { answerKey = new { q1 = "A" } })
            )
            .Value;
        var act2 = LearningActivity
            .Create(classId, "Upload", LearningActivityComponentType.FileUpload, 40, 2)
            .Value;
        var now = DateTimeOffset.UtcNow;
        var participant = LearningParticipant
            .Enroll(classId, Guid.CreateVersion7(), LearningParticipantRole.Student, now)
            .Value;

        var c1 = LearningActivityCompletion.Submit(
            act1,
            participant,
            dueDateUtc: null,
            now,
            submissionDataJson: JsonSerializer.Serialize(new { answers = new { q1 = "A" } }),
            binaryFileId: null,
            studentComment: null
        );
        var c2 = LearningActivityCompletion.Submit(
            act2,
            participant,
            dueDateUtc: null,
            now,
            submissionDataJson: null,
            binaryFileId: Guid.CreateVersion7(),
            studentComment: null
        );

        var passedBeforeGrading = participant.RecalculateProgress([act1, act2], [c1, c2], sys, now);
        Assert.False(passedBeforeGrading);
        Assert.Equal(LearningCompletionStatus.Incomplete, participant.LearningCompletionStatus);

        Assert.True(c2.Grade(30, 40, Guid.CreateVersion7(), "Well done", now).IsSuccess);
        var passedAfterGrading = participant.RecalculateProgress([act1, act2], [c1, c2], sys, now);
        Assert.True(passedAfterGrading);
        Assert.Equal(LearningCompletionStatus.Pass, participant.LearningCompletionStatus);
        Assert.Equal(90m, participant.CurrentGradePercent);
        Assert.Equal("Pass", participant.CurrentGradeScaleName);

        // Resubmission updates submission state cleanly and PersonId repointing works
        c2.ApplyResubmission(
            act2,
            now.AddHours(1),
            submissionDataJson: null,
            binaryFileId: Guid.CreateVersion7(),
            studentComment: "Revised PDF"
        );
        Assert.True(c2.RequiresGrading);
        var newPersonId = Guid.CreateVersion7();
        c2.RepointPerson(participant.PersonId, newPersonId);
        Assert.Equal(newPersonId, c2.PersonId);
    }
}
