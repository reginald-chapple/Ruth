using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Assessments;

/// <summary>Tracks a person's pending or completed assessment with cooldown and expiration rules.</summary>
public sealed class Assessment : AggregateRoot<AssessmentId>, IAuditableEntity, ISecured
{
    /// <summary>Canonical entity type key for Assessment.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.Assessment";

    private Assessment() { }

    private Assessment(
        AssessmentId id,
        PersonId personId,
        AssessmentTypeId assessmentTypeId,
        PersonId? requesterPersonId,
        DateTimeOffset requestedOnUtc,
        DateOnly? requestedDueDate
    )
        : base(id)
    {
        PersonId = personId;
        AssessmentTypeId = assessmentTypeId;
        RequesterPersonId = requesterPersonId;
        RequestedOnUtc = requestedOnUtc.ToUniversalTime();
        RequestedDueDate = requestedDueDate;
        Status = AssessmentStatus.Pending;
    }

    /// <summary>Gets the target person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the assessment type identifier.</summary>
    public AssessmentTypeId AssessmentTypeId { get; private set; }

    /// <summary>Gets the optional person who requested this assessment.</summary>
    public PersonId? RequesterPersonId { get; private set; }

    /// <summary>Gets the UTC timestamp when requested.</summary>
    public DateTimeOffset? RequestedOnUtc { get; private set; }

    /// <summary>Gets the optional due date.</summary>
    public DateOnly? RequestedDueDate { get; private set; }

    /// <summary>Gets the current assessment status.</summary>
    public AssessmentStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when completed.</summary>
    public DateTimeOffset? CompletedOnUtc { get; private set; }

    /// <summary>Gets the structured JSON result payload.</summary>
    public string? ResultDataJson { get; private set; }

    /// <summary>Gets a concise human-readable summary (e.g. "DISC: D/I", "Top Gift: Leadership").</summary>
    public string? SummaryText { get; private set; }

    /// <summary>Gets the last reminder timestamp.</summary>
    public DateTimeOffset? LastReminderOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            AssessmentType.EntityTypeKeyName,
            AssessmentTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Evaluates whether this completed assessment has expired per <paramref name="validDurationDays" />.</summary>
    public bool IsExpiredAt(int validDurationDays, DateTimeOffset nowUtc)
    {
        return validDurationDays > 0
            && Status == AssessmentStatus.Complete
            && CompletedOnUtc.HasValue
            && CompletedOnUtc.Value.AddDays(validDurationDays) < nowUtc.ToUniversalTime();
    }

    /// <summary>Creates a new pending assessment enforcing requester requirement and retake cooldown.</summary>
    public static Result<Assessment> Request(
        PersonId personId,
        AssessmentType assessmentType,
        PersonId? requesterPersonId,
        DateOnly? requestedDueDate,
        DateTimeOffset? lastCompletedOnUtc,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(assessmentType);

        return assessmentType.RequiresRequest && !requesterPersonId.HasValue
                ? (Result<Assessment>)CrmErrors.AssessmentRequiresRequest
            : !requesterPersonId.HasValue
            && assessmentType.MinimumDaysToRetake > 0
            && lastCompletedOnUtc.HasValue
            && lastCompletedOnUtc.Value.AddDays(assessmentType.MinimumDaysToRetake)
                > nowUtc.ToUniversalTime()
                ? (Result<Assessment>)CrmErrors.AssessmentCooldownActive
            : Result.Success(
                new Assessment(
                    AssessmentId.Create(),
                    personId,
                    assessmentType.Id,
                    requesterPersonId,
                    nowUtc,
                    requestedDueDate
                )
            );
    }

    /// <summary>Marks this assessment complete with JSON results and an optional summary.</summary>
    public Result Complete(
        string resultDataJson,
        string? summaryText,
        DateTimeOffset completedOnUtc
    )
    {
        if (string.IsNullOrWhiteSpace(resultDataJson))
        {
            return CrmErrors.AssessmentTypeInvalid;
        }

        var validatedSummary = CrmTextPolicy.ValidateOptionalText(
            summaryText,
            250,
            allowNewLines: false
        );
        if (validatedSummary.IsFailure)
        {
            return Result.Failure(validatedSummary.Errors);
        }

        Status = AssessmentStatus.Complete;
        CompletedOnUtc = completedOnUtc.ToUniversalTime();
        ResultDataJson = resultDataJson.Trim();
        SummaryText = validatedSummary.Value;
        return Result.Success();
    }

    internal void RepointPerson(PersonId targetPersonId)
    {
        PersonId = targetPersonId;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
