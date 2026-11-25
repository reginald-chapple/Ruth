using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.CheckIn.Domain.Configurations;

/// <summary>Aggregate root defining a check-in configuration profile, its areas, eligibility rules, and room matrices.</summary>
public sealed class CheckInConfiguration
    : AggregateRoot<CheckInConfigurationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for CheckInConfiguration.</summary>
    public const string EntityTypeKeyName = "Ruth.CheckIn.CheckInConfiguration";

    private readonly List<CheckInArea> _areas = [];

    private CheckInConfiguration() { }

    private CheckInConfiguration(CheckInConfigurationId id)
        : base(id) { }

    /// <summary>Gets the unique check-in configuration name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name used for uniqueness enforcement.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the operational scenario category.</summary>
    public CheckInProfileKind ProfileKind { get; private set; }

    /// <summary>Gets the optional Branch / Site scope.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the allowed kiosk/station search mode.</summary>
    public CheckInSearchMode SearchMode { get; private set; }

    /// <summary>Gets minutes prior to schedule start when check-in opens.</summary>
    public int CheckInStartOffsetMinutes { get; private set; } = 30;

    /// <summary>Gets minutes after schedule start when check-in closes.</summary>
    public int CheckInEndOffsetMinutes { get; private set; } = 30;

    /// <summary>Gets the number of alphabetic characters in generated security pickup codes.</summary>
    public int SecurityCodeAlphaLength { get; private set; } = 2;

    /// <summary>Gets the number of numeric digits in generated security pickup codes.</summary>
    public int SecurityCodeNumericLength { get; private set; } = 2;

    /// <summary>Gets whether duplicate check-ins within the same schedule window are blocked.</summary>
    public bool PreventDuplicateCheckIn { get; private set; } = true;

    /// <summary>Gets whether participant check-out tracking is enabled.</summary>
    public bool AllowCheckout { get; private set; } = true;

    /// <summary>Gets the attendance lookback window in days for pre-selecting recent areas.</summary>
    public int AutoSelectDaysBack { get; private set; } = 60;

    /// <summary>Gets whether the configuration profile is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the configured check-in areas.</summary>
    public IReadOnlyCollection<CheckInArea> Areas => _areas.AsReadOnly();

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

    internal static Result<CheckInConfiguration> Create(CheckInConfigurationInput input)
    {
        var config = new CheckInConfiguration(CheckInConfigurationId.Create());
        var updated = config.Update(input);
        return updated.IsFailure
            ? Result.Failure<CheckInConfiguration>(updated.Errors)
            : Result.Success(config);
    }

    internal Result Update(CheckInConfigurationInput input)
    {
        var nameResult = CheckInTextPolicy.NormalizeRequired(
            input.Name,
            CheckInTextPolicy.NameMaxLength,
            CheckInErrors.ConfigurationNameRequired,
            CheckInErrors.ConfigurationNameTooLong
        );
        var descResult = CheckInTextPolicy.NormalizeOptional(
            input.Description,
            CheckInTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        var requiresPickupCode =
            input.ProfileKind == CheckInProfileKind.YouthAndChildcare
            || _areas.Exists(static a => a.IssueSecurityPickupCode);
        var constraintResult = ValidateSettingsConstraints(input, requiresPickupCode);
        if (constraintResult.IsFailure)
        {
            return constraintResult;
        }

        ApplyValidatedScalars(input, nameResult.Value, descResult.Value);
        return Result.Success();
    }

    internal Result<CheckInArea> UpsertArea(
        CheckInAreaSpec spec,
        CheckInAreaId? targetAreaId = null
    )
    {
        var guardResult = ValidateAreaUpsertGuards(spec, targetAreaId);
        return guardResult.IsFailure ? Result.Failure<CheckInArea>(guardResult.Errors)
            : targetAreaId is { } existingId && existingId != CheckInAreaId.Empty
                ? UpdateExistingArea(existingId, spec)
            : CreateAndAttachArea(spec);
    }

    private Result ValidateAreaUpsertGuards(CheckInAreaSpec spec, CheckInAreaId? targetAreaId)
    {
        var missingEntropy =
            spec.IssueSecurityPickupCode && SecurityCodeAlphaLength + SecurityCodeNumericLength < 3;
        return missingEntropy ? CheckInErrors.SecurityCodeTooShortForPickup
            : HasDuplicateAreaName(spec.Name, targetAreaId) ? CheckInErrors.AreaDuplicateName
            : Result.Success();
    }

    internal Result RemoveArea(CheckInAreaId areaId)
    {
        var index = _areas.FindIndex(a => a.Id == areaId);
        if (index < 0)
        {
            return CheckInErrors.AreaNotFound;
        }

        _areas.RemoveAt(index);
        return Result.Success();
    }

    internal Result SetRoomAvailability(
        CheckInAreaId areaId,
        Guid locationId,
        bool isOpenForCheckIn,
        int? softCapacity,
        int? firmCapacity
    )
    {
        var area = _areas.Find(a => a.Id == areaId);
        return area is null
            ? CheckInErrors.AreaNotFound
            : area.SetRoomAvailability(locationId, isOpenForCheckIn, softCapacity, firmCapacity);
    }

    private bool HasDuplicateAreaName(string? rawName, CheckInAreaId? targetAreaId)
    {
        var candidateName = string.IsNullOrWhiteSpace(rawName)
            ? string.Empty
            : rawName.Trim().ToUpperInvariant();
        return _areas.Exists(a =>
            (!targetAreaId.HasValue || a.Id != targetAreaId.Value)
            && string.Equals(a.NormalizedName, candidateName, StringComparison.Ordinal)
        );
    }

    private Result<CheckInArea> UpdateExistingArea(CheckInAreaId existingId, CheckInAreaSpec spec)
    {
        var existing = _areas.Find(a => a.Id == existingId);
        if (existing is null)
        {
            return Result.Failure<CheckInArea>(CheckInErrors.AreaNotFound);
        }

        var updated = existing.Apply(spec);
        return updated.IsFailure
            ? Result.Failure<CheckInArea>(updated.Errors)
            : Result.Success(existing);
    }

    private Result<CheckInArea> CreateAndAttachArea(CheckInAreaSpec spec)
    {
        var created = CheckInArea.Create(Id, spec);
        if (created.IsFailure)
        {
            return Result.Failure<CheckInArea>(created.Errors);
        }

        _areas.Add(created.Value);
        return created;
    }

    private static Result ValidateSettingsConstraints(
        CheckInConfigurationInput input,
        bool requiresPickupCode
    )
    {
        if (!IsValidCheckInWindow(input.CheckInStartOffsetMinutes, input.CheckInEndOffsetMinutes))
        {
            return CheckInErrors.InvalidCheckInWindowOffsets;
        }

        var codeResult = ValidateSecurityCodeLengths(
            input.SecurityCodeAlphaLength,
            input.SecurityCodeNumericLength,
            requiresPickupCode
        );
        return codeResult.IsFailure ? codeResult
            : input.AutoSelectDaysBack is >= 0 and <= 365 ? Result.Success()
            : CheckInErrors.InvalidAutoSelectDaysBack;
    }

    private static bool IsValidCheckInWindow(int startOffsetMinutes, int endOffsetMinutes)
    {
        return startOffsetMinutes is >= 0 and <= 720
            && endOffsetMinutes is >= -120 and <= 720
            && startOffsetMinutes + endOffsetMinutes > 0;
    }

    private static Result ValidateSecurityCodeLengths(int alpha, int numeric, bool requiresPickup)
    {
        var total = alpha + numeric;
        var invalidBounds =
            !IsSingleCodeSegmentValid(alpha) || !IsSingleCodeSegmentValid(numeric) || total > 8;
        return invalidBounds ? CheckInErrors.InvalidSecurityCodeLength
            : requiresPickup && total < 3 ? CheckInErrors.SecurityCodeTooShortForPickup
            : Result.Success();
    }

    private static bool IsSingleCodeSegmentValid(int length)
    {
        return length is >= 0 and <= 6;
    }

    private void ApplyValidatedScalars(
        CheckInConfigurationInput input,
        string normalizedName,
        string? description
    )
    {
        Name = normalizedName;
        NormalizedName = normalizedName.ToUpperInvariant();
        Description = description;
        ProfileKind = input.ProfileKind;
        BranchId = input.BranchId is { } bid && bid != Guid.Empty ? bid : null;
        SearchMode = input.SearchMode;
        CheckInStartOffsetMinutes = input.CheckInStartOffsetMinutes;
        CheckInEndOffsetMinutes = input.CheckInEndOffsetMinutes;
        SecurityCodeAlphaLength = input.SecurityCodeAlphaLength;
        SecurityCodeNumericLength = input.SecurityCodeNumericLength;
        PreventDuplicateCheckIn = input.PreventDuplicateCheckIn;
        AllowCheckout = input.AllowCheckout;
        AutoSelectDaysBack = input.AutoSelectDaysBack;
        IsActive = input.IsActive;
    }
}
