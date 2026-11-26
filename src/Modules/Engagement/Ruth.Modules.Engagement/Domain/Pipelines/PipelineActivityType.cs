using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Configurable activity type logged against pipeline engagement requests.</summary>
public sealed class PipelineActivityType : Entity<PipelineActivityTypeId>
{
    private PipelineActivityType() { }

    private PipelineActivityType(PipelineActivityTypeId id, PipelineTypeId pipelineTypeId)
        : base(id)
    {
        PipelineTypeId = pipelineTypeId;
    }

    public PipelineTypeId PipelineTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal static PipelineActivityType Create(
        PipelineTypeId pipelineTypeId,
        PipelineActivityTypeSpec spec
    )
    {
        var id =
            spec.Id is { } existing && existing != Guid.Empty
                ? PipelineActivityTypeId.From(existing)
                : PipelineActivityTypeId.Create();
        var item = new PipelineActivityType(id, pipelineTypeId);
        item.ApplySpec(spec);
        return item;
    }

    internal void ApplySpec(PipelineActivityTypeSpec spec)
    {
        Name = spec.Name.Trim();
        Order = spec.Order;
        IsActive = spec.IsActive;
    }
}
