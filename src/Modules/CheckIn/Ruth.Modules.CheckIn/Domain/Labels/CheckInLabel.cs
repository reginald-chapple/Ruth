using System.Text.RegularExpressions;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.CheckIn.Domain.Labels;

/// <summary>Aggregate root representing a check-in thermal badge or receipt label definition and its ZPL layout.</summary>
public sealed partial class CheckInLabel
    : AggregateRoot<CheckInLabelId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for CheckInLabel.</summary>
    public const string EntityTypeKeyName = "Ruth.CheckIn.CheckInLabel";

    private CheckInLabel() { }

    private CheckInLabel(CheckInLabelId id, bool isSystem)
        : base(id)
    {
        IsSystem = isSystem;
    }

    /// <summary>Gets the display name of the label.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the unique lowercase slug key.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the functional category of this label.</summary>
    public CheckInLabelKind LabelKind { get; private set; }

    /// <summary>Gets the optional scoped CheckInConfiguration identifier.</summary>
    public Guid? ConfigurationId { get; private set; }

    /// <summary>Gets the optional scoped CheckInArea identifier.</summary>
    public Guid? AreaId { get; private set; }

    /// <summary>Gets the structured visual and ZPL layout schema.</summary>
    public CheckInLabelLayoutSchema Layout { get; private set; } =
        CheckInLabelLayoutSchema
            .Create(CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto())
            .Value;

    /// <summary>Gets the optional expert raw ZPL II template override.</summary>
    public string? CustomZplOverride { get; private set; }

    /// <summary>Gets the ascending print order.</summary>
    public int PrintOrder { get; private set; }

    /// <summary>Gets the number of copies printed per trigger (1..5).</summary>
    public int Copies { get; private set; } = 1;

    /// <summary>Gets whether the label is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether the label is a seeded system template protected from deletion.</summary>
    public bool IsSystem { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
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

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    internal static Result<CheckInLabel> Create(CheckInLabelSpec spec)
    {
        var label = new CheckInLabel(CheckInLabelId.Create(), spec.IsSystem);
        var updated = label.Update(spec);
        return updated.IsFailure
            ? Result.Failure<CheckInLabel>(updated.Errors)
            : Result.Success(label);
    }

    internal Result Update(CheckInLabelSpec spec)
    {
        var nameResult = CheckInTextPolicy.NormalizeRequired(
            spec.Name,
            CheckInTextPolicy.NameMaxLength,
            CheckInErrors.LabelNameRequired,
            CheckInErrors.LabelNameTooLong
        );
        var keyResult = NormalizeKey(spec.Key);
        var descResult = CheckInTextPolicy.NormalizeOptional(
            spec.Description,
            CheckInTextPolicy.DescriptionMaxLength
        );
        var hasCustomZpl = !string.IsNullOrWhiteSpace(spec.CustomZplOverride);
        var layoutResult = CheckInLabelLayoutSchema.Create(
            spec.Layout,
            allowEmptyElements: hasCustomZpl
        );
        var combined = Result.Combine(nameResult, keyResult, descResult, layoutResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (spec.Copies is < 1 or > 5)
        {
            return CheckInErrors.InvalidLabelCopies;
        }

        ApplyValidated(
            spec,
            nameResult.Value,
            keyResult.Value,
            descResult.Value,
            layoutResult.Value,
            hasCustomZpl ? spec.CustomZplOverride!.Trim() : null
        );
        return Result.Success();
    }

    internal Result EnsureDeletable()
    {
        return IsSystem ? CheckInErrors.SystemLabelCannotBeDeleted : Result.Success();
    }

    private void ApplyValidated(
        CheckInLabelSpec spec,
        string name,
        string key,
        string? description,
        CheckInLabelLayoutSchema layout,
        string? customZpl
    )
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Key = key;
        Description = description;
        LabelKind = spec.LabelKind;
        ConfigurationId = NormalizeOptionalGuid(spec.ConfigurationId);
        AreaId = NormalizeOptionalGuid(spec.AreaId);
        Layout = layout;
        CustomZplOverride = customZpl;
        PrintOrder = spec.PrintOrder;
        Copies = spec.Copies;
        IsActive = spec.IsActive;
    }

    private static Result<string> NormalizeKey(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return Result.Failure<string>(CheckInErrors.LabelKeyInvalid);
        }

        var normalized = rawKey.Trim().ToLowerInvariant();
        return LabelKeyRegex.IsMatch(normalized)
            ? Result.Success(normalized)
            : Result.Failure<string>(CheckInErrors.LabelKeyInvalid);
    }

    private static Guid? NormalizeOptionalGuid(Guid? raw)
    {
        return raw is { } id && id != Guid.Empty ? id : null;
    }

    [GeneratedRegex(
        "^[a-z0-9][a-z0-9-]{1,79}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex LabelKeyRegex { get; }
}
