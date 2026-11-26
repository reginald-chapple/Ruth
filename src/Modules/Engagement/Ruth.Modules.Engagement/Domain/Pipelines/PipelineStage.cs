using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Ordered stage within a PipelineType.</summary>
public sealed class PipelineStage : Entity<PipelineStageId>
{
    private List<StageAutomation> _automations = [];

    private PipelineStage() { }

    private PipelineStage(PipelineStageId id, PipelineTypeId pipelineTypeId)
        : base(id)
    {
        PipelineTypeId = pipelineTypeId;
    }

    public PipelineTypeId PipelineTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public bool IsInitial { get; private set; }
    public bool IsTerminalCompleted { get; private set; }
    public bool IsTerminalInactive { get; private set; }
    public int? IdleThresholdDaysOverride { get; private set; }

    public IReadOnlyList<StageAutomation> Automations
    {
        get => _automations;
        private set => _automations = [.. value];
    }

    internal static PipelineStage Create(PipelineTypeId pipelineTypeId, PipelineStageSpec spec)
    {
        var existingId = NormalizeGuid(spec.Id);
        var id = existingId.HasValue
            ? PipelineStageId.From(existingId.Value)
            : PipelineStageId.Create();
        var stage = new PipelineStage(id, pipelineTypeId);
        stage.ApplySpec(spec);
        return stage;
    }

    internal void ApplySpec(PipelineStageSpec spec)
    {
        Name = spec.Name.Trim();
        Description = TrimOptional(spec.Description, 1000);
        Order = spec.Order;
        IsInitial = spec.IsInitial;
        IsTerminalCompleted = spec.IsTerminalCompleted;
        IsTerminalInactive = spec.IsTerminalInactive && !spec.IsTerminalCompleted;
        IdleThresholdDaysOverride = spec.IdleThresholdDaysOverride;
        _automations = BuildAutomations(spec.Automations);
    }

    private static List<StageAutomation> BuildAutomations(IReadOnlyList<StageAutomationDto>? specs)
    {
        if (specs is null || specs.Count == 0)
        {
            return [];
        }

        var list = new List<StageAutomation>(specs.Count);
        foreach (var item in specs)
        {
            if (item.AutomationKind != StageAutomationKind.None || item.AutoSetFollowUpDays is > 0)
            {
                list.Add(MapAutomation(item));
            }
        }

        return list;
    }

    private static StageAutomation MapAutomation(StageAutomationDto dto)
    {
        var followUpDays = dto.AutoSetFollowUpDays is > 0 ? dto.AutoSetFollowUpDays : null;
        return new StageAutomation(
            dto.AutomationKind,
            NormalizeGuid(dto.TargetEntityId),
            TrimOptional(dto.SystemCommunicationKey, 120),
            followUpDays
        );
    }
}
