using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Milestone step definition within a JourneyProgram.</summary>
public sealed class MilestoneType : Entity<MilestoneTypeId>
{
    private List<Guid> _prerequisiteMilestoneTypeIds = [];

    private MilestoneType() { }

    private MilestoneType(MilestoneTypeId id, JourneyProgramId journeyProgramId)
        : base(id)
    {
        JourneyProgramId = journeyProgramId;
    }

    public JourneyProgramId JourneyProgramId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconCssClass { get; private set; }
    public int Order { get; private set; }
    public bool IsRequiredForProgramCompletion { get; private set; } = true;
    public bool AllowMultipleCompletions { get; private set; }
    public bool HasEndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<Guid> PrerequisiteMilestoneTypeIds
    {
        get => _prerequisiteMilestoneTypeIds;
        private set => _prerequisiteMilestoneTypeIds = [.. value];
    }

    internal static MilestoneType Create(JourneyProgramId programId, MilestoneTypeSpec spec)
    {
        var id =
            spec.Id is { } existing && existing != Guid.Empty
                ? MilestoneTypeId.From(existing)
                : MilestoneTypeId.Create();
        var item = new MilestoneType(id, programId);
        item.ApplySpec(spec);
        return item;
    }

    internal void ApplySpec(MilestoneTypeSpec spec)
    {
        Name = spec.Name.Trim();
        Code = spec.Code.Trim().ToUpperInvariant();
        Description = TrimOptional(spec.Description, 1000);
        IconCssClass = TrimOptional(spec.IconCssClass, 100);
        Order = spec.Order;
        IsRequiredForProgramCompletion = spec.IsRequiredForProgramCompletion;
        AllowMultipleCompletions = spec.AllowMultipleCompletions;
        HasEndDate = spec.HasEndDate;
        IsActive = spec.IsActive;
    }

    internal void SetPrerequisites(IEnumerable<Guid> prerequisiteIds)
    {
        _prerequisiteMilestoneTypeIds =
        [
            .. prerequisiteIds.Where(id => id != Guid.Empty && id != Id.Value).Distinct(),
        ];
    }
}
