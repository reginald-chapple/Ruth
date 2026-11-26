using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Primitives;

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
        var id =
            spec.Id is { } existing && existing != Guid.Empty
                ? PipelineStageId.From(existing)
                : PipelineStageId.Create();
        var stage = new PipelineStage(id, pipelineTypeId);
        stage.ApplySpec(spec);
        return stage;
    }

    internal void ApplySpec(PipelineStageSpec spec)
    {
        Name = spec.Name.Trim();
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim();
        Order = spec.Order;
        IsInitial = spec.IsInitial;
        IsTerminalCompleted = spec.IsTerminalCompleted;
        IsTerminalInactive = spec.IsTerminalInactive && !spec.IsTerminalCompleted;
        IdleThresholdDaysOverride = spec.IdleThresholdDaysOverride;
        _automations =
            spec.Automations is null
                ? []
                :
                [
                    .. spec.Automations
                        .Where(static a =>
                            a.AutomationKind != StageAutomationKind.None
                            || a.AutoSetFollowUpDays is > 0
                        )
                        .Select(static a => new StageAutomation(
                            a.AutomationKind,
                            a.TargetEntityId is { } tid && tid != Guid.Empty ? tid : null,
                            string.IsNullOrWhiteSpace(a.SystemCommunicationKey)
                                ? null
                                : a.SystemCommunicationKey.Trim(),
                            a.AutoSetFollowUpDays is > 0 ? a.AutoSetFollowUpDays : null
                        )),
                ];
    }
}
