using System.Globalization;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>Learner completion &amp; facilitator grading record for a LearningActivity.</summary>
public sealed class LearningActivityCompletion
    : AggregateRoot<LearningActivityCompletionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private LearningActivityCompletion() { }

    private LearningActivityCompletion(LearningActivityCompletionId id)
        : base(id) { }

    public LearningClassId LearningClassId { get; private set; }
    public LearningActivityId LearningActivityId { get; private set; }
    public LearningParticipantId LearningParticipantId { get; private set; }
    public Guid PersonId { get; private set; }
    public bool IsStudentCompleted { get; private set; }
    public bool IsFacilitatorCompleted { get; private set; }
    public bool RequiresGrading { get; private set; }
    public int PointsEarned { get; private set; }
    public DateTimeOffset? DueDateTimeUtc { get; private set; }
    public DateTimeOffset? CompletedDateTimeUtc { get; private set; }
    public bool WasCompletedOnTime { get; private set; } = true;
    public string? StudentComment { get; private set; }
    public string? FacilitatorComment { get; private set; }
    public string? SubmissionDataJson { get; private set; }
    public Guid? BinaryFileId { get; private set; }
    public Guid? GradedByPersonId { get; private set; }
    public DateTimeOffset? GradedDateTimeUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static LearningActivityCompletion Submit(
        LearningActivity activity,
        LearningParticipant participant,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset nowUtc,
        string? submissionDataJson,
        Guid? binaryFileId,
        string? studentComment
    )
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(participant);

        var outcome = AssessmentAutoGrader.EvaluateSubmission(activity, submissionDataJson);
        var completedUtc = nowUtc.ToUniversalTime();
        var onTime = dueDateUtc is null || completedUtc <= dueDateUtc.Value;

        return new LearningActivityCompletion(LearningActivityCompletionId.Create())
        {
            LearningClassId = activity.LearningClassId,
            LearningActivityId = activity.Id,
            LearningParticipantId = participant.Id,
            PersonId = participant.PersonId,
            IsStudentCompleted = true,
            IsFacilitatorCompleted = outcome.IsFacilitatorCompleted,
            RequiresGrading = outcome.RequiresGrading,
            PointsEarned = outcome.PointsEarned,
            DueDateTimeUtc = dueDateUtc?.ToUniversalTime(),
            CompletedDateTimeUtc = completedUtc,
            WasCompletedOnTime = onTime,
            SubmissionDataJson = string.IsNullOrWhiteSpace(submissionDataJson)
                ? null
                : submissionDataJson.Trim(),
            BinaryFileId = binaryFileId,
            StudentComment = string.IsNullOrWhiteSpace(studentComment)
                ? null
                : studentComment.Trim(),
        };
    }

    public void ApplyResubmission(
        LearningActivity activity,
        DateTimeOffset nowUtc,
        string? submissionDataJson,
        Guid? binaryFileId,
        string? studentComment
    )
    {
        ArgumentNullException.ThrowIfNull(activity);
        var outcome = AssessmentAutoGrader.EvaluateSubmission(activity, submissionDataJson);
        var completedUtc = nowUtc.ToUniversalTime();

        IsStudentCompleted = true;
        IsFacilitatorCompleted = outcome.IsFacilitatorCompleted;
        RequiresGrading = outcome.RequiresGrading;
        PointsEarned = outcome.PointsEarned;
        CompletedDateTimeUtc = completedUtc;
        WasCompletedOnTime = DueDateTimeUtc is null || completedUtc <= DueDateTimeUtc.Value;
        SubmissionDataJson = string.IsNullOrWhiteSpace(submissionDataJson)
            ? null
            : submissionDataJson.Trim();
        BinaryFileId = binaryFileId;
        StudentComment = string.IsNullOrWhiteSpace(studentComment) ? null : studentComment.Trim();
    }

    public Result Grade(
        int pointsEarned,
        int maxPoints,
        Guid facilitatorPersonId,
        string? facilitatorComment,
        DateTimeOffset nowUtc
    )
    {
        if (pointsEarned < 0 || pointsEarned > maxPoints)
        {
            return Result.Failure(
                LmsErrors.Validation(
                    "Grading.PointsOutOfRange",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Awarded points must be between 0 and {maxPoints}."
                    )
                )
            );
        }

        PointsEarned = pointsEarned;
        IsFacilitatorCompleted = true;
        RequiresGrading = false;
        GradedByPersonId = facilitatorPersonId;
        GradedDateTimeUtc = nowUtc.ToUniversalTime();
        FacilitatorComment = string.IsNullOrWhiteSpace(facilitatorComment)
            ? null
            : facilitatorComment.Trim();
        return Result.Success();
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }

        if (GradedByPersonId == retiredPersonId)
        {
            GradedByPersonId = survivingPersonId;
        }
    }
}
