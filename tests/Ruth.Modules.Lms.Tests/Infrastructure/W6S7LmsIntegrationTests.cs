using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.Modules.Lms.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Lms.Tests.Infrastructure;

[Collection(LmsFixtures.Name)]
public sealed class W6S7LmsIntegrationTests(LmsContainersFixture containers)
{
    [Fact]
    public async Task Lms_EndToEnd_Prerequisites_AutoGrade_FacilitatorQueue_OutboxAndPersonMerge()
    {
        var conn = await containers.CreateIsolatedDatabaseAsync();
        await using var provider = await LmsTestHostFactory.CreateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();

        var seeded = await LmsCurriculumSeedHelper.SeedAsync(commands);
        var learnerId = Guid.CreateVersion7();
        var facilitatorId = Guid.CreateVersion7();

        var blockedEnroll = await commands.SendAsync<
            EnrollLearningParticipantCommand,
            LearningParticipantResponse
        >(new EnrollLearningParticipantCommand(seeded.Class2Id, learnerId));
        Assert.True(blockedEnroll.IsFailure);

        var uploadCompletionId = await EnrollAndSubmitActivitiesAsync(
            commands,
            seeded.Class1Id,
            seeded.QuizId,
            seeded.UploadId,
            learnerId
        );
        await VerifyGradingQueuePassAndUnblockAsync(
            commands,
            queries,
            db,
            seeded,
            uploadCompletionId,
            learnerId,
            facilitatorId
        );
        await VerifyPersonMergeRepointsLearnerAsync(scope.ServiceProvider, db, learnerId);
    }

    private static async Task<Guid> EnrollAndSubmitActivitiesAsync(
        ICommandDispatcher commands,
        Guid class1Id,
        Guid quizId,
        Guid uploadId,
        Guid learnerId
    )
    {
        var enroll1 = await commands.SendAsync<
            EnrollLearningParticipantCommand,
            LearningParticipantResponse
        >(new EnrollLearningParticipantCommand(class1Id, learnerId));
        Assert.True(enroll1.IsSuccess);

        var quizSub = await commands.SendAsync<
            SubmitLearningActivityCommand,
            LearningActivityCompletionResponse
        >(
            new SubmitLearningActivityCommand(
                class1Id,
                quizId,
                learnerId,
                SubmissionDataJson: JsonSerializer.Serialize(
                    new { answers = new { q1 = "Yes", q2 = "A" } }
                )
            )
        );
        Assert.True(quizSub.IsSuccess);
        Assert.False(quizSub.Value.RequiresGrading);
        Assert.Equal(50, quizSub.Value.PointsEarned);

        var uploadSub = await commands.SendAsync<
            SubmitLearningActivityCommand,
            LearningActivityCompletionResponse
        >(
            new SubmitLearningActivityCommand(
                class1Id,
                uploadId,
                learnerId,
                BinaryFileId: Guid.CreateVersion7(),
                StudentComment: "Attached completed checklist."
            )
        );
        Assert.True(uploadSub.IsSuccess);
        Assert.True(uploadSub.Value.RequiresGrading);
        return uploadSub.Value.Id;
    }

    private static async Task VerifyGradingQueuePassAndUnblockAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        LmsDbContext db,
        LmsCurriculumSeedHelper.SeededCurriculum seeded,
        Guid uploadCompletionId,
        Guid learnerId,
        Guid facilitatorId
    )
    {
        var queue = await queries.QueryAsync<
            GetFacilitatorGradingQueueQuery,
            IReadOnlyList<LearningActivityCompletionResponse>
        >(new GetFacilitatorGradingQueueQuery(seeded.Class1Id));
        Assert.True(queue.IsSuccess);
        _ = Assert.Single(queue.Value);

        var graded = await commands.SendAsync<
            GradeLearningActivityCommand,
            LearningActivityCompletionResponse
        >(new GradeLearningActivityCommand(uploadCompletionId, 45, facilitatorId, "Verified!"));
        Assert.True(graded.IsSuccess);
        Assert.False(graded.Value.RequiresGrading);
        Assert.Equal(45, graded.Value.PointsEarned);

        var outboxEvents = await db
            .OutboxMessages.AsNoTracking()
            .Where(m => m.EventType.Contains(nameof(LearningCourseCompletedIntegrationEvent)))
            .ToListAsync();
        _ = Assert.Single(outboxEvents);

        var catalog = await queries.QueryAsync<
            GetPublicCourseCatalogQuery,
            IReadOnlyList<PublicCourseCatalogItemResponse>
        >(new GetPublicCourseCatalogQuery(learnerId));
        Assert.True(catalog.IsSuccess);
        var c2Catalog = Assert.Single(catalog.Value, c => c.Course.Id == seeded.Course2Id);
        Assert.True(c2Catalog.PrerequisitesMet);

        var enroll2 = await commands.SendAsync<
            EnrollLearningParticipantCommand,
            LearningParticipantResponse
        >(new EnrollLearningParticipantCommand(seeded.Class2Id, learnerId));
        Assert.True(enroll2.IsSuccess);

        var portal = await queries.QueryAsync<
            GetLearnerPortalQuery,
            IReadOnlyList<LearnerClassWorkspaceResponse>
        >(new GetLearnerPortalQuery(learnerId));
        Assert.True(portal.IsSuccess);
        Assert.Equal(2, portal.Value.Count);
        Assert.Contains(portal.Value, w =>
            w.ClassDetail.LearningCourseId == seeded.Course1Id
            && w.Participant.CompletionStatus == LearningCompletionStatus.Pass
            && w.Participant.CurrentGradePercent == 95m
        );
    }

    private static async Task VerifyPersonMergeRepointsLearnerAsync(
        IServiceProvider sp,
        LmsDbContext db,
        Guid retiredPersonId
    )
    {
        var survivingPersonId = Guid.CreateVersion7();
        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingPersonId,
                retiredPersonId,
                [],
                HadMultipleDistinctEmails: false
            )
        );

        var remainingRetired = await db
            .Participants.AsNoTracking()
            .CountAsync(p => p.PersonId == retiredPersonId);
        var survivingCount = await db
            .Participants.AsNoTracking()
            .CountAsync(p => p.PersonId == survivingPersonId);
        Assert.Equal(0, remainingRetired);
        Assert.Equal(2, survivingCount);
    }
}
