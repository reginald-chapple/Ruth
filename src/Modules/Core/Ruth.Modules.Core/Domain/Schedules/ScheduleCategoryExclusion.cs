using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Date-range blackout/exclusion applying to all schedules in a <see cref="Category" />.</summary>
public sealed class ScheduleCategoryExclusion
    : AggregateRoot<ScheduleCategoryExclusionId>,
        IAuditableEntity,
        ISecured
{
    /// <summary>Canonical entity type key for ScheduleCategoryExclusion.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.ScheduleCategoryExclusion";

    private ScheduleCategoryExclusion() { }

    private ScheduleCategoryExclusion(
        ScheduleCategoryExclusionId id,
        CategoryId categoryId,
        string title,
        DateOnly startDate,
        DateOnly endDate
    )
        : base(id)
    {
        CategoryId = categoryId;
        Title = title;
        StartDate = startDate;
        EndDate = endDate;
        StartOnUtc = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        EndOnUtc = new DateTimeOffset(endDate.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero);
    }

    /// <summary>Gets the owning schedule category identifier.</summary>
    public CategoryId CategoryId { get; private set; }

    /// <summary>Gets the exclusion title (e.g., Christmas Holiday).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the inclusive start date.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Gets the inclusive end date.</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Gets the normalized start timestamp (00:00:00 UTC).</summary>
    public DateTimeOffset StartOnUtc { get; private set; }

    /// <summary>Gets the normalized inclusive end timestamp (23:59:59 UTC).</summary>
    public DateTimeOffset EndOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            Category.EntityTypeKeyName,
            CategoryId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated inclusive schedule category exclusion.</summary>
    public static Result<ScheduleCategoryExclusion> Create(
        CategoryId categoryId,
        string title,
        DateOnly startDate,
        DateOnly endDate
    )
    {
        if (categoryId == CategoryId.Empty)
        {
            return CoreErrors.CategoryNotFound;
        }

        var titleResult = CoreTextPolicy.ValidateRequiredText(
            title,
            maxLength: 50,
            CoreScheduleErrors.ExclusionTitleRequired,
            CoreScheduleErrors.ExclusionTitleTooLong
        );
        return titleResult.IsFailure ? Result.Failure<ScheduleCategoryExclusion>(titleResult.Errors)
            : endDate < startDate
                ? (Result<ScheduleCategoryExclusion>)CoreScheduleErrors.ExclusionDateRangeInvalid
            : (Result<ScheduleCategoryExclusion>)
                new ScheduleCategoryExclusion(
                    ScheduleCategoryExclusionId.Create(),
                    categoryId,
                    titleResult.Value,
                    startDate,
                    endDate
                );
    }
}
