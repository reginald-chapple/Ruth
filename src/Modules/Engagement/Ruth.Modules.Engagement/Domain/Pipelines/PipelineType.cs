using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Organization-agnostic onboarding, volunteer intake, mentorship, or referral pipeline definition.</summary>
public sealed partial class PipelineType
    : AggregateRoot<PipelineTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.PipelineType";

    private readonly List<PipelineStage> _stages = [];
    private readonly List<PipelineActivityType> _activityTypes = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private PipelineType() { }

    private PipelineType(PipelineTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool EnableFutureFollowUp { get; private set; } = true;
    public int DefaultFollowUpDays { get; private set; } = 7;
    public int DaysUntilRequestIdle { get; private set; } = 14;
    public bool RequiresPlacementGroup { get; private set; }
    public bool IsSequentialStageProgression { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<PipelineStage> Stages => _stages;
    public IReadOnlyList<PipelineActivityType> ActivityTypes => _activityTypes;

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<PipelineType> Create(PipelineTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var pipeline = new PipelineType(PipelineTypeId.Create());
        var res = pipeline.Update(spec);
        return res.IsFailure ? Result.Failure<PipelineType>(res.Errors) : pipeline;
    }

    public Result Update(PipelineTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsValidHeader(spec, slug) || !AreStagesAndActivitiesValid(spec))
        {
            return EngagementErrors.PipelineTypeInvalid;
        }

        Name = spec.Name.Trim();
        Slug = slug;
        Description = TrimOptional(spec.Description, 2000);
        EnableFutureFollowUp = spec.EnableFutureFollowUp;
        DefaultFollowUpDays = spec.DefaultFollowUpDays;
        DaysUntilRequestIdle = spec.DaysUntilRequestIdle;
        RequiresPlacementGroup = spec.RequiresPlacementGroup;
        IsSequentialStageProgression = spec.IsSequentialStageProgression;
        IsActive = spec.IsActive;

        SyncStages(spec.Stages);
        SyncActivityTypes(spec.ActivityTypes ?? []);
        ApplyAttributes(spec.Attributes);
        return Result.Success();
    }

    public PipelineStage GetInitialStage()
    {
        return _stages.OrderBy(static s => s.Order).FirstOrDefault(static s => s.IsInitial)
            ?? _stages.OrderBy(static s => s.Order).First();
    }

    public PipelineStage? FindStage(Guid stageId) =>
        _stages.FirstOrDefault(s => s.Id.Value == stageId);

    private void SyncStages(IReadOnlyList<PipelineStageSpec> stageSpecs)
    {
        var ordered = stageSpecs.OrderBy(static s => s.Order).ToList();
        var retainedIds = new HashSet<Guid>();

        foreach (var spec in ordered)
        {
            var existing = spec.Id.HasValue
                ? _stages.FirstOrDefault(s => s.Id.Value == spec.Id.Value)
                : null;
            if (existing is not null)
            {
                existing.ApplySpec(spec);
                _ = retainedIds.Add(existing.Id.Value);
            }
            else
            {
                var created = PipelineStage.Create(Id, spec);
                _stages.Add(created);
                _ = retainedIds.Add(created.Id.Value);
            }
        }

        _ = _stages.RemoveAll(s => !retainedIds.Contains(s.Id.Value));
    }

    private void SyncActivityTypes(IReadOnlyList<PipelineActivityTypeSpec> specs)
    {
        var retainedIds = new HashSet<Guid>();
        foreach (var spec in specs.OrderBy(static a => a.Order))
        {
            var existing = spec.Id.HasValue
                ? _activityTypes.FirstOrDefault(a => a.Id.Value == spec.Id.Value)
                : null;
            if (existing is not null)
            {
                existing.ApplySpec(spec);
                _ = retainedIds.Add(existing.Id.Value);
            }
            else
            {
                var created = PipelineActivityType.Create(Id, spec);
                _activityTypes.Add(created);
                _ = retainedIds.Add(created.Id.Value);
            }
        }

        _ = _activityTypes.RemoveAll(a => !retainedIds.Contains(a.Id.Value));
    }

    private static bool IsValidHeader(PipelineTypeSpec spec, string slug)
    {
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && spec.DefaultFollowUpDays is >= 1 and <= 365
            && spec.DaysUntilRequestIdle is >= 1 and <= 365;
    }

    private static bool AreStagesAndActivitiesValid(PipelineTypeSpec spec)
    {
        if (spec.Stages is null || spec.Stages.Count == 0)
        {
            return false;
        }

        var hasInitial = spec.Stages.Any(static s => s.IsInitial);
        var validStages = spec.Stages.All(static s =>
            !string.IsNullOrWhiteSpace(s.Name)
            && s.Name.Trim().Length <= 150
            && s.Order >= 0
            && (!s.IdleThresholdDaysOverride.HasValue || s.IdleThresholdDaysOverride.Value is >= 1 and <= 365)
        );
        var validActivities =
            spec.ActivityTypes is null
            || spec.ActivityTypes.All(static a =>
                !string.IsNullOrWhiteSpace(a.Name) && a.Name.Trim().Length <= 150 && a.Order >= 0
            );

        return hasInitial && validStages && validActivities;
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var (key, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributeValues[key.Trim()] = value;
            }
        }
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }
}
