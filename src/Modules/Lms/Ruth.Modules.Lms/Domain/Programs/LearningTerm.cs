using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Programs;

/// <summary>Academic/Cohort LearningTerm belonging to a LearningProgram.</summary>
public sealed class LearningTerm
    : AggregateRoot<LearningTermId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningTerm";

    private LearningTerm() { }

    private LearningTerm(LearningTermId id)
        : base(id) { }

    public LearningProgramId LearningProgramId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PublicName { get; private set; } = string.Empty;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? EnrollmentCloseDate { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<LearningTerm> Create(
        LearningProgramId programId,
        string name,
        string? publicName = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        DateOnly? enrollmentCloseDate = null
    )
    {
        return programId.IsEmpty
                ? Result.Failure<LearningTerm>(
                    LmsErrors.Validation("Term.ProgramRequired", "LearningProgramId is required.")
                )
            : string.IsNullOrWhiteSpace(name)
                ? Result.Failure<LearningTerm>(
                    LmsErrors.Validation("Term.NameRequired", "LearningTerm name is required.")
                )
            : startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value
                ? Result.Failure<LearningTerm>(
                    LmsErrors.Validation(
                        "Term.DatesInvalid",
                        "Term EndDate cannot precede StartDate."
                    )
                )
            : Result.Success(
                new LearningTerm(LearningTermId.Create())
                {
                    LearningProgramId = programId,
                    Name = name.Trim(),
                    PublicName = string.IsNullOrWhiteSpace(publicName)
                        ? name.Trim()
                        : publicName.Trim(),
                    StartDate = startDate,
                    EndDate = endDate,
                    EnrollmentCloseDate = enrollmentCloseDate,
                    IsActive = true,
                }
            );
    }
}
