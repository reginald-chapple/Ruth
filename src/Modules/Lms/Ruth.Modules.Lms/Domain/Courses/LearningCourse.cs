using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Lms.Domain.Courses;

/// <summary>LearningCourse aggregate with prerequisite graph invariants.</summary>
public sealed class LearningCourse
    : AggregateRoot<LearningCourseId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningCourse";

    private readonly List<LearningCourseRequirement> _requirements = [];

    private LearningCourse() { }

    private LearningCourse(LearningCourseId id)
        : base(id) { }

    public LearningProgramId LearningProgramId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PublicName { get; private set; } = string.Empty;
    public string CourseCode { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public int Credits { get; private set; }
    public int? MaxStudents { get; private set; }
    public bool AllowHistoricalAccess { get; private set; } = true;
    public bool EnableAnnouncements { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public int Order { get; private set; }

    public IReadOnlyCollection<LearningCourseRequirement> Requirements =>
        _requirements.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

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

    public static Result<LearningCourse> Create(
        LearningProgramId programId,
        string name,
        string courseCode,
        string? publicName = null,
        string? summary = null,
        string? description = null,
        int credits = 0,
        int? maxStudents = null,
        bool allowHistoricalAccess = true,
        bool enableAnnouncements = true,
        bool isPublic = true,
        int order = 0
    )
    {
        var validation = ValidateInputs(programId, name, courseCode, credits, maxStudents);
        return validation.IsFailure
            ? Result.Failure<LearningCourse>(validation.PrimaryError)
            : Result.Success(
                new LearningCourse(LearningCourseId.Create())
                {
                    LearningProgramId = programId,
                    Name = name.Trim(),
                    PublicName = string.IsNullOrWhiteSpace(publicName)
                        ? name.Trim()
                        : publicName.Trim(),
                    CourseCode = courseCode.Trim().ToUpperInvariant(),
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    Credits = credits,
                    MaxStudents = maxStudents,
                    AllowHistoricalAccess = allowHistoricalAccess,
                    EnableAnnouncements = enableAnnouncements,
                    IsPublic = isPublic,
                    IsActive = true,
                    Order = order,
                }
            );
    }

    public Result<LearningCourseRequirement> AddRequirement(
        LearningCourseId requiredCourseId,
        CourseRequirementType requirementType
    )
    {
        if (requiredCourseId == Id)
        {
            return Result.Failure<LearningCourseRequirement>(
                LmsErrors.Validation(
                    "CourseRequirement.SelfReference",
                    "A course cannot require itself."
                )
            );
        }

        if (_requirements.Exists(r => r.RequiredLearningCourseId == requiredCourseId))
        {
            return Result.Failure<LearningCourseRequirement>(
                LmsErrors.Conflict(
                    "CourseRequirement.Duplicate",
                    "This required course is already linked."
                )
            );
        }

        var req = new LearningCourseRequirement(Id, requiredCourseId, requirementType);
        _requirements.Add(req);
        return Result.Success(req);
    }

    private static Result ValidateInputs(
        LearningProgramId programId,
        string name,
        string courseCode,
        int credits,
        int? maxStudents
    )
    {
        return programId.IsEmpty
                ? Result.Failure(
                    LmsErrors.Validation("Course.ProgramRequired", "LearningProgramId is required.")
                )
            : string.IsNullOrWhiteSpace(name)
                ? Result.Failure(
                    LmsErrors.Validation("Course.NameRequired", "Course name is required.")
                )
            : string.IsNullOrWhiteSpace(courseCode)
                ? Result.Failure(
                    LmsErrors.Validation("Course.CodeRequired", "Course code is required.")
                )
            : credits < 0
                ? Result.Failure(
                    LmsErrors.Validation("Course.CreditsInvalid", "Credits cannot be negative.")
                )
            : maxStudents is <= 0
                ? Result.Failure(
                    LmsErrors.Validation(
                        "Course.MaxStudentsInvalid",
                        "MaxStudents must be greater than zero."
                    )
                )
            : Result.Success();
    }
}
