using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Records a constituent's full completion of a JourneyProgram.</summary>
public sealed class JourneyCompletion
    : AggregateRoot<JourneyCompletionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private JourneyCompletion() { }

    private JourneyCompletion(JourneyCompletionId id)
        : base(id) { }

    public Guid PersonId { get; private set; }
    public Guid JourneyProgramId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly StartedDate { get; private set; }
    public DateOnly CompletedDate { get; private set; }

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

    public static JourneyCompletion Create(
        Guid personId,
        Guid journeyProgramId,
        Guid? branchId,
        DateOnly startedDate,
        DateOnly completedDate
    )
    {
        return new JourneyCompletion(JourneyCompletionId.Create())
        {
            PersonId = personId,
            JourneyProgramId = journeyProgramId,
            BranchId = NormalizeGuid(branchId),
            StartedDate = startedDate <= completedDate ? startedDate : completedDate,
            CompletedDate = completedDate,
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
