using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>LearningClass aggregate owning content pages, announcements, and ordered activities.</summary>
public sealed class LearningClass
    : AggregateRoot<LearningClassId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningClass";

    private readonly List<LearningClassContentPage> _contentPages = [];
    private readonly List<LearningClassAnnouncement> _announcements = [];
    private readonly List<LearningActivity> _activities = [];

    private LearningClass() { }

    private LearningClass(LearningClassId id)
        : base(id) { }

    public LearningCourseId LearningCourseId { get; private set; }
    public LearningTermId? LearningTermId { get; private set; }
    public LearningGradingSystemId? LearningGradingSystemId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? VirtualRoomId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public bool TakesAttendance { get; private set; }
    public bool AllowOverbooking { get; private set; }
    public int? MaxStudents { get; private set; }

    public IReadOnlyCollection<LearningClassContentPage> ContentPages => _contentPages.AsReadOnly();
    public IReadOnlyCollection<LearningClassAnnouncement> Announcements =>
        _announcements.AsReadOnly();
    public IReadOnlyCollection<LearningActivity> Activities => _activities.AsReadOnly();

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

    public static Result<LearningClass> Create(
        LearningCourseId courseId,
        string name,
        LearningTermId? termId = null,
        LearningGradingSystemId? gradingSystemId = null,
        Guid? branchId = null,
        Guid? virtualRoomId = null,
        bool isPublic = true,
        bool takesAttendance = false,
        bool allowOverbooking = false,
        int? maxStudents = null
    )
    {
        return courseId.IsEmpty
                ? Result.Failure<LearningClass>(
                    LmsErrors.Validation("Class.CourseRequired", "LearningCourseId is required.")
                )
            : string.IsNullOrWhiteSpace(name)
                ? Result.Failure<LearningClass>(
                    LmsErrors.Validation("Class.NameRequired", "LearningClass name is required.")
                )
            : maxStudents is <= 0
                ? Result.Failure<LearningClass>(
                    LmsErrors.Validation(
                        "Class.MaxStudentsInvalid",
                        "MaxStudents must be greater than zero."
                    )
                )
            : Result.Success(
                new LearningClass(LearningClassId.Create())
                {
                    LearningCourseId = courseId,
                    LearningTermId = termId,
                    LearningGradingSystemId = gradingSystemId,
                    BranchId = branchId,
                    VirtualRoomId = virtualRoomId,
                    Name = name.Trim(),
                    IsPublic = isPublic,
                    IsActive = true,
                    TakesAttendance = takesAttendance,
                    AllowOverbooking = allowOverbooking,
                    MaxStudents = maxStudents,
                }
            );
    }

    public Result<LearningClassContentPage> AddContentPage(
        string title,
        string contentHtml,
        int order,
        DateTimeOffset? startDateTimeUtc = null
    )
    {
        var pageRes = LearningClassContentPage.Create(
            Id,
            title,
            contentHtml,
            order,
            startDateTimeUtc
        );
        if (pageRes.IsSuccess)
        {
            _contentPages.Add(pageRes.Value);
        }

        return pageRes;
    }

    public Result<LearningClassAnnouncement> AddAnnouncement(
        string title,
        string descriptionHtml,
        DateTimeOffset publishDateTimeUtc,
        string? summary = null
    )
    {
        var annRes = LearningClassAnnouncement.Create(
            Id,
            title,
            descriptionHtml,
            publishDateTimeUtc,
            summary
        );
        if (annRes.IsSuccess)
        {
            _announcements.Add(annRes.Value);
        }

        return annRes;
    }

    public Result<LearningActivity> AddActivity(AddLearningActivityCommand spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var actRes = LearningActivity.Create(
            Id,
            spec.Name,
            spec.ComponentType,
            spec.Points,
            spec.Order,
            spec.Description,
            spec.AssignTo,
            spec.AvailabilityCriteria,
            spec.AvailableDateDefault,
            spec.AvailableDateOffsetDays,
            spec.DueDateCriteria,
            spec.DueDateDefault,
            spec.DueDateOffsetDays,
            spec.IsStudentCommentingEnabled,
            spec.ActivityConfigJson
        );
        if (actRes.IsSuccess)
        {
            _activities.Add(actRes.Value);
        }

        return actRes;
    }
}
