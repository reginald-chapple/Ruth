using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Organization-agnostic milestone progression program (e.g., Leadership Pathway, Onboarding Track).</summary>
public sealed partial class JourneyProgram
    : AggregateRoot<JourneyProgramId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.JourneyProgram";

    private readonly List<MilestoneStatus> _statuses = [];
    private readonly List<MilestoneType> _milestoneTypes = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private JourneyProgram() { }

    private JourneyProgram(JourneyProgramId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public JourneyViewMode DefaultListView { get; private set; }
    public JourneyCompletionMode CompletionMode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyList<MilestoneStatus> Statuses => _statuses;
    public IReadOnlyList<MilestoneType> MilestoneTypes => _milestoneTypes;

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

    public static Result<JourneyProgram> Create(JourneyProgramSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var program = new JourneyProgram(JourneyProgramId.Create());
        var res = program.Update(spec);
        return res.IsFailure ? Result.Failure<JourneyProgram>(res.Errors) : program;
    }

    public Result Update(JourneyProgramSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsValidSpec(spec, slug))
        {
            return EngagementErrors.JourneyProgramInvalid;
        }

        Name = spec.Name.Trim();
        Slug = slug;
        Description = TrimOptional(spec.Description, 2000);
        CategoryId = NormalizeGuid(spec.CategoryId);
        DefaultListView = spec.DefaultListView;
        CompletionMode = spec.CompletionMode;
        IsActive = spec.IsActive;

        SyncStatuses(spec.Statuses);
        SyncMilestones(spec.MilestoneTypes);
        ApplyAttributes(_attributeValues, spec.Attributes);
        return Result.Success();
    }

    public MilestoneStatus? FindStatus(Guid statusId)
    {
        return _statuses.FirstOrDefault(s => s.Id.Value == statusId);
    }

    public MilestoneType? FindMilestoneType(Guid milestoneTypeId)
    {
        return _milestoneTypes.FirstOrDefault(m => m.Id.Value == milestoneTypeId);
    }

    private void SyncStatuses(IReadOnlyList<MilestoneStatusSpec> specs)
    {
        var retained = new HashSet<Guid>();
        foreach (var spec in specs.OrderBy(static s => s.Order))
        {
            var existing = spec.Id.HasValue
                ? _statuses.FirstOrDefault(s => s.Id.Value == spec.Id.Value)
                : null;
            if (existing is not null)
            {
                existing.ApplySpec(spec);
                _ = retained.Add(existing.Id.Value);
            }
            else
            {
                var created = MilestoneStatus.Create(Id, spec);
                _statuses.Add(created);
                _ = retained.Add(created.Id.Value);
            }
        }

        _ = _statuses.RemoveAll(s => !retained.Contains(s.Id.Value));
    }

    private void SyncMilestones(IReadOnlyList<MilestoneTypeSpec> specs)
    {
        UpsertMilestoneEntities(specs);
        WireMilestonePrerequisites(specs);
    }

    private void UpsertMilestoneEntities(IReadOnlyList<MilestoneTypeSpec> specs)
    {
        var retained = new HashSet<Guid>();
        foreach (var spec in specs.OrderBy(static m => m.Order))
        {
            var existing = spec.Id.HasValue
                ? _milestoneTypes.FirstOrDefault(m => m.Id.Value == spec.Id.Value)
                : null;
            if (existing is not null)
            {
                existing.ApplySpec(spec);
                _ = retained.Add(existing.Id.Value);
            }
            else
            {
                var created = MilestoneType.Create(Id, spec);
                _milestoneTypes.Add(created);
                _ = retained.Add(created.Id.Value);
            }
        }

        _ = _milestoneTypes.RemoveAll(m => !retained.Contains(m.Id.Value));
    }

    private void WireMilestonePrerequisites(IReadOnlyList<MilestoneTypeSpec> specs)
    {
        var byCode = _milestoneTypes.ToDictionary(
            static m => m.Code,
            static m => m.Id.Value,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var spec in specs)
        {
            var code = spec.Code.Trim().ToUpperInvariant();
            var target = _milestoneTypes.First(m =>
                string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase)
            );
            var prereqIds = (spec.PrerequisiteCodes ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c) && byCode.ContainsKey(c.Trim()))
                .Select(c => byCode[c.Trim()]);
            target.SetPrerequisites(prereqIds);
        }
    }

    private static bool IsValidSpec(JourneyProgramSpec spec, string slug)
    {
        var validStatuses =
            spec.Statuses is { Count: > 0 }
            && spec.Statuses.Any(static s => s.IsCompleteStatus)
            && spec.Statuses.All(static s => !string.IsNullOrWhiteSpace(s.Name));

        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && validStatuses
            && JourneyPrerequisiteValidator.ValidateAcyclicPrerequisites(spec.MilestoneTypes);
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }
}
