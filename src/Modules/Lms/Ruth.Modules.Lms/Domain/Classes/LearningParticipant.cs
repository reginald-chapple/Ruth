using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>Participant (Student or Facilitator) enrolled in a LearningClass with gradebook rollup.</summary>
public sealed class LearningParticipant
    : AggregateRoot<LearningParticipantId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private LearningParticipant() { }

    private LearningParticipant(LearningParticipantId id)
        : base(id) { }

    public LearningClassId LearningClassId { get; private set; }
    public Guid PersonId { get; private set; }
    public LearningParticipantRole Role { get; private set; }
    public LearningCompletionStatus LearningCompletionStatus { get; private set; }
    public DateTimeOffset? LearningCompletionDateTimeUtc { get; private set; }
    public decimal CurrentGradePercent { get; private set; }
    public string? CurrentGradeScaleName { get; private set; }
    public DateTimeOffset EnrolledDateTimeUtc { get; private set; }

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

    public static Result<LearningParticipant> Enroll(
        LearningClassId classId,
        Guid personId,
        LearningParticipantRole role,
        DateTimeOffset nowUtc
    )
    {
        return classId.IsEmpty || personId == Guid.Empty
            ? Result.Failure<LearningParticipant>(
                LmsErrors.Validation(
                    "Participant.IdsRequired",
                    "LearningClassId and PersonId are required."
                )
            )
            : Result.Success(
                new LearningParticipant(LearningParticipantId.Create())
                {
                    LearningClassId = classId,
                    PersonId = personId,
                    Role = role,
                    LearningCompletionStatus = LearningCompletionStatus.Incomplete,
                    CurrentGradePercent = 0m,
                    EnrolledDateTimeUtc = nowUtc.ToUniversalTime(),
                }
            );
    }

    public bool RecalculateProgress(
        IReadOnlyCollection<LearningActivity> classActivities,
        IReadOnlyCollection<LearningActivityCompletion> completions,
        LearningGradingSystem? gradingSystem,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(classActivities);
        ArgumentNullException.ThrowIfNull(completions);

        if (Role != LearningParticipantRole.Student)
        {
            return false;
        }

        var studentActivities = classActivities
            .Where(static a => a.AssignTo == LearningActivityAssignTo.Student)
            .ToList();
        if (studentActivities.Count == 0)
        {
            return false;
        }

        var (earnedPoints, possiblePoints, allCompletedAndGraded) = TallyScores(
            studentActivities,
            completions
        );
        CurrentGradePercent = ComputeGradePercent(
            earnedPoints,
            possiblePoints,
            allCompletedAndGraded
        );

        var scale = gradingSystem?.EvaluateScale(CurrentGradePercent);
        CurrentGradeScaleName = scale?.Name;
        var isPassing = scale?.IsPassing ?? (CurrentGradePercent >= 70m);

        var wasPassed = LearningCompletionStatus == LearningCompletionStatus.Pass;
        ApplyCompletionOutcome(allCompletedAndGraded, isPassing, nowUtc);
        return !wasPassed && LearningCompletionStatus == LearningCompletionStatus.Pass;
    }

    private static (int Earned, int Possible, bool AllDone) TallyScores(
        List<LearningActivity> studentActivities,
        IReadOnlyCollection<LearningActivityCompletion> completions
    )
    {
        var byActivity = completions.ToDictionary(static c => c.LearningActivityId);
        var earned = 0;
        var allDone = true;
        foreach (var act in studentActivities)
        {
            if (
                byActivity.TryGetValue(act.Id, out var comp)
                && comp.IsStudentCompleted
                && !comp.RequiresGrading
            )
            {
                earned += comp.PointsEarned;
            }
            else
            {
                allDone = false;
            }
        }

        return (earned, studentActivities.Sum(static a => a.Points), allDone);
    }

    private static decimal ComputeGradePercent(int earned, int possible, bool allDone)
    {
        var raw =
            possible > 0
                ? Math.Round((decimal)earned / possible * 100m, 2, MidpointRounding.AwayFromZero)
            : allDone ? 100m
            : 0m;
        return Math.Clamp(raw, 0m, 100m);
    }

    private void ApplyCompletionOutcome(bool allDone, bool isPassing, DateTimeOffset nowUtc)
    {
        if (!allDone)
        {
            LearningCompletionStatus = LearningCompletionStatus.Incomplete;
            return;
        }

        LearningCompletionStatus = isPassing
            ? LearningCompletionStatus.Pass
            : LearningCompletionStatus.Fail;
        LearningCompletionDateTimeUtc ??= nowUtc.ToUniversalTime();
    }

    public void RepointPerson(Guid survivingPersonId)
    {
        PersonId = survivingPersonId;
    }
}
