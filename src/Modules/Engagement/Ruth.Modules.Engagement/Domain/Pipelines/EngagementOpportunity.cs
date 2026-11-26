using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Concrete volunteer, onboarding, mentorship, or referral opportunity within a PipelineType.</summary>
public sealed partial class EngagementOpportunity
    : AggregateRoot<EngagementOpportunityId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Engagement.EngagementOpportunity";

    private List<OpportunityBranchCoordinatorConfig> _branchConfigs = [];
    private List<OpportunityPlacementGroupConfig> _placementGroupConfigs = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private EngagementOpportunity() { }

    private EngagementOpportunity(EngagementOpportunityId id)
        : base(id) { }

    public Guid PipelineTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? PublicSummary { get; private set; }
    public string? Description { get; private set; }
    public string? IconCssClass { get; private set; }
    public Guid? PhotoBinaryFileId { get; private set; }
    public Guid? DefaultBranchId { get; private set; }
    public Guid? DefaultConnectorPersonId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyList<OpportunityBranchCoordinatorConfig> BranchConfigs
    {
        get => _branchConfigs;
        private set => _branchConfigs = [.. value];
    }

    public IReadOnlyList<OpportunityPlacementGroupConfig> PlacementGroupConfigs
    {
        get => _placementGroupConfigs;
        private set => _placementGroupConfigs = [.. value];
    }

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

    public static Result<EngagementOpportunity> Create(EngagementOpportunitySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var opportunity = new EngagementOpportunity(EngagementOpportunityId.Create());
        var res = opportunity.Update(spec);
        return res.IsFailure ? Result.Failure<EngagementOpportunity>(res.Errors) : opportunity;
    }

    public Result Update(EngagementOpportunitySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        if (
            spec.PipelineTypeId == Guid.Empty
            || string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 200
            || !SlugRegex.IsMatch(slug)
        )
        {
            return EngagementErrors.OpportunityInvalid;
        }

        PipelineTypeId = spec.PipelineTypeId;
        Name = spec.Name.Trim();
        Slug = slug;
        PublicSummary = TrimOptional(spec.PublicSummary, 500);
        Description = TrimOptional(spec.Description, 4000);
        IconCssClass = TrimOptional(spec.IconCssClass, 100);
        PhotoBinaryFileId = NormalizeGuid(spec.PhotoBinaryFileId);
        DefaultBranchId = NormalizeGuid(spec.DefaultBranchId);
        DefaultConnectorPersonId = NormalizeGuid(spec.DefaultConnectorPersonId);
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
        ApplyBranchAndPlacementConfigs(spec);
        ApplyAttributes(_attributeValues, spec.Attributes);
        return Result.Success();
    }

    public Guid? ResolveDefaultConnector(Guid? branchId)
    {
        var effectiveBranchId = branchId ?? DefaultBranchId;
        if (effectiveBranchId.HasValue)
        {
            var match = _branchConfigs.FirstOrDefault(b =>
                b.BranchId == effectiveBranchId.Value && b.DefaultConnectorPersonId.HasValue
            );
            if (match is not null)
            {
                return match.DefaultConnectorPersonId;
            }
        }

        return DefaultConnectorPersonId;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var modified = false;
        if (DefaultConnectorPersonId == retiredPersonId)
        {
            DefaultConnectorPersonId = survivingPersonId;
            modified = true;
        }

        if (_branchConfigs.Exists(b => b.DefaultConnectorPersonId == retiredPersonId))
        {
            _branchConfigs =
            [
                .. _branchConfigs.Select(b =>
                    b.DefaultConnectorPersonId == retiredPersonId
                        ? b with
                        {
                            DefaultConnectorPersonId = survivingPersonId,
                        }
                        : b
                ),
            ];
            modified = true;
        }

        return modified;
    }

    private void ApplyBranchAndPlacementConfigs(EngagementOpportunitySpec spec)
    {
        _branchConfigs = spec.BranchConfigs is null
            ? []
            :
            [
                .. spec.BranchConfigs.Select(static b => new OpportunityBranchCoordinatorConfig(
                    NormalizeGuid(b.BranchId),
                    NormalizeGuid(b.DefaultConnectorPersonId),
                    NormalizeGuid(b.ConnectorGroupId)
                )),
            ];

        _placementGroupConfigs = spec.PlacementGroupConfigs is null
            ? []
            :
            [
                .. spec
                    .PlacementGroupConfigs.Where(static p => p.GroupId != Guid.Empty)
                    .Select(static p => new OpportunityPlacementGroupConfig(
                        p.GroupId,
                        NormalizeGuid(p.GroupRoleId),
                        NormalizeGuid(p.BranchId),
                        p.UseAllGroupsOfType
                    )),
            ];
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }
}
