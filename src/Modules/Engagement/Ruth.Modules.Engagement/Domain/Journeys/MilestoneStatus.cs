using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Status option (e.g., In Progress, Completed) within a JourneyProgram.</summary>
public sealed class MilestoneStatus : Entity<MilestoneStatusId>
{
    private MilestoneStatus() { }

    private MilestoneStatus(MilestoneStatusId id, JourneyProgramId journeyProgramId)
        : base(id)
    {
        JourneyProgramId = journeyProgramId;
    }

    public JourneyProgramId JourneyProgramId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ColorHex { get; private set; } = "#0F6CBD";
    public bool IsCompleteStatus { get; private set; }
    public int Order { get; private set; }

    internal static MilestoneStatus Create(JourneyProgramId programId, MilestoneStatusSpec spec)
    {
        var id =
            spec.Id is { } existing && existing != Guid.Empty
                ? MilestoneStatusId.From(existing)
                : MilestoneStatusId.Create();
        var status = new MilestoneStatus(id, programId);
        status.ApplySpec(spec);
        return status;
    }

    internal void ApplySpec(MilestoneStatusSpec spec)
    {
        Name = spec.Name.Trim();
        ColorHex = string.IsNullOrWhiteSpace(spec.ColorHex) ? "#0F6CBD" : spec.ColorHex.Trim();
        IsCompleteStatus = spec.IsCompleteStatus;
        Order = spec.Order;
    }
}
