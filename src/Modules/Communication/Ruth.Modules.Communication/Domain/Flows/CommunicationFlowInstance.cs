using System.Globalization;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Cohort or rolling wave execution instance of a <see cref="CommunicationFlow" />.</summary>
public sealed class CommunicationFlowInstance
    : AggregateRoot<CommunicationFlowInstanceId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private CommunicationFlowInstance() { }

    private CommunicationFlowInstance(
        CommunicationFlowInstanceId id,
        CommunicationFlowId flowId,
        string cohortLabel,
        DateTimeOffset startDateUtc
    )
        : base(id)
    {
        FlowId = flowId;
        CohortLabel = cohortLabel;
        Status = FlowInstanceStatus.Active;
        StartDateUtc = startDateUtc.ToUniversalTime();
    }

    public CommunicationFlowId FlowId { get; private set; }
    public string CohortLabel { get; private set; } = string.Empty;
    public FlowInstanceStatus Status { get; private set; }
    public DateTimeOffset StartDateUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static CommunicationFlowInstance Create(
        CommunicationFlowId flowId,
        string? cohortLabel,
        DateTimeOffset startDateUtc
    )
    {
        var label = string.IsNullOrWhiteSpace(cohortLabel)
            ? string.Create(CultureInfo.InvariantCulture, $"Cohort {startDateUtc:yyyy-MM-dd}")
            : cohortLabel.Trim();
        if (label.Length > 150)
        {
            label = label[..150];
        }

        return new CommunicationFlowInstance(
            CommunicationFlowInstanceId.Create(),
            flowId,
            label,
            startDateUtc
        );
    }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        if (Status == FlowInstanceStatus.Active)
        {
            Status = FlowInstanceStatus.Completed;
            CompletedAtUtc = completedAtUtc.ToUniversalTime();
        }
    }
}
