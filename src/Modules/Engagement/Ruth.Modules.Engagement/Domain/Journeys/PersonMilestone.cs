using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Constituent's recorded milestone step within a JourneyProgram.</summary>
public sealed class PersonMilestone
    : AggregateRoot<PersonMilestoneId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.PersonMilestone";

    private PersonMilestone() { }

    private PersonMilestone(PersonMilestoneId id)
        : base(id) { }

    public Guid PersonId { get; private set; }
    public Guid JourneyProgramId { get; private set; }
    public Guid MilestoneTypeId { get; private set; }
    public Guid MilestoneStatusId { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? CompletedDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Note { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();
    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action) =>
        GlobalDefaultSecured.Instance.IsAllowedByDefault(action);

    public void SetCreated(DateTimeOffset createdOnUtc) =>
        CreatedOnUtc = createdOnUtc.ToUniversalTime();

    public void SetModified(DateTimeOffset modifiedOnUtc) =>
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();

    public void SetConcurrencyStamp(Guid newStamp) => ConcurrencyStamp = newStamp;

    public static Result<PersonMilestone> Record(
        Guid personId,
        JourneyProgram program,
        MilestoneType milestoneType,
        MilestoneStatus status,
        DateOnly startDate,
        DateOnly? completedDate,
        DateOnly? endDate,
        Guid? branchId,
        string? note,
        IReadOnlySet<Guid> alreadyCompletedTypeIds
    )
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(milestoneType);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(alreadyCompletedTypeIds);

        if (
            personId == Guid.Empty
            || milestoneType.JourneyProgramId != program.Id
            || status.JourneyProgramId != program.Id
            || (endDate.HasValue && endDate.Value < startDate)
        )
        {
            return EngagementErrors.PersonMilestoneInvalid;
        }

        if (status.IsCompleteStatus)
        {
            if (
                !milestoneType.AllowMultipleCompletions
                && alreadyCompletedTypeIds.Contains(milestoneType.Id.Value)
            )
            {
                return EngagementErrors.MilestoneDuplicateNotAllowed;
            }

            if (!JourneyPrerequisiteValidator.ArePrerequisitesMet(milestoneType, alreadyCompletedTypeIds))
            {
                return EngagementErrors.MilestonePrerequisiteUnmet;
            }
        }

        return new PersonMilestone(PersonMilestoneId.Create())
        {
            PersonId = personId,
            JourneyProgramId = program.Id.Value,
            MilestoneTypeId = milestoneType.Id.Value,
            MilestoneStatusId = status.Id.Value,
            IsCompleted = status.IsCompleteStatus,
            BranchId = NormalizeGuid(branchId),
            StartDate = startDate,
            CompletedDate = status.IsCompleteStatus ? completedDate ?? startDate : null,
            EndDate = milestoneType.HasEndDate ? endDate : null,
            Note = TrimOptional(note, 1000),
        };
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
