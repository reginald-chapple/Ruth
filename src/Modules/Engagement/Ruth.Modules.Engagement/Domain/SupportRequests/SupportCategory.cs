using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.SupportRequests;

/// <summary>Organization-agnostic category for Community Care, Member Assistance, Wellbeing, or Mentorship intake.</summary>
public sealed partial class SupportCategory
    : AggregateRoot<SupportCategoryId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.SupportCategory";

    private SupportCategory() { }

    private SupportCategory(SupportCategoryId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool RequiresModeration { get; private set; } = true;
    public int DefaultExpirationDays { get; private set; } = 14;
    public bool AllowAnonymousIntake { get; private set; } = true;
    public bool AllowPublicCommunityWall { get; private set; } = true;
    public bool AllowComments { get; private set; } = true;
    public int AutoFlagHoldThreshold { get; private set; } = 3;
    public Guid? DefaultCoordinatorPersonId { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<SupportCategory> Create(SupportCategorySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new SupportCategory(SupportCategoryId.Create());
        var res = entity.Update(spec);
        return res.IsFailure ? Result.Failure<SupportCategory>(res.Errors) : entity;
    }

    public Result Update(SupportCategorySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsValidSpec(spec, slug))
        {
            return EngagementErrors.SupportCategoryInvalid;
        }

        Name = spec.Name.Trim();
        Slug = slug;
        Description = TrimOptional(spec.Description, 1000);
        RequiresModeration = spec.RequiresModeration;
        DefaultExpirationDays = spec.DefaultExpirationDays;
        AllowAnonymousIntake = spec.AllowAnonymousIntake;
        AllowPublicCommunityWall = spec.AllowPublicCommunityWall;
        AllowComments = spec.AllowComments;
        AutoFlagHoldThreshold = spec.AutoFlagHoldThreshold;
        DefaultCoordinatorPersonId = NormalizeGuid(spec.DefaultCoordinatorPersonId);
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static bool IsValidSpec(SupportCategorySpec spec, string slug)
    {
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && spec.DefaultExpirationDays is >= 1 and <= 365
            && spec.AutoFlagHoldThreshold is >= 1 and <= 100;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (DefaultCoordinatorPersonId != retiredPersonId)
        {
            return false;
        }

        DefaultCoordinatorPersonId = survivingPersonId;
        return true;
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }
}
