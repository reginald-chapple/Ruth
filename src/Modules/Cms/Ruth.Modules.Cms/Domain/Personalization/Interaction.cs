using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Immutable telemetry record representing a user or visitor interaction event.</summary>
public sealed class Interaction : AggregateRoot<InteractionId>, IAuditableEntity
{
    private Dictionary<string, string?> _interactionData = new(StringComparer.OrdinalIgnoreCase);

    private Interaction() { }

    private Interaction(InteractionId id)
        : base(id) { }

    public InteractionComponentId InteractionComponentId { get; private set; }
    public InteractionSessionId? InteractionSessionId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string Operation { get; private set; } = "View";
    public string? Summary { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? ChannelCustom1 { get; private set; }
    public string? ChannelCustom2 { get; private set; }
    public DateTimeOffset OccurredDateTimeUtc { get; private set; }

    public IReadOnlyDictionary<string, string?> InteractionData
    {
        get => _interactionData;
        private set =>
            _interactionData = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public static Result<Interaction> Create(
        InteractionComponentId componentId,
        string operation,
        DateTimeOffset occurredUtc,
        InteractionSessionId? sessionId = null,
        Guid? personId = null,
        string? summary = null,
        string? sourceUrl = null,
        string? channelCustom1 = null,
        string? channelCustom2 = null,
        IReadOnlyDictionary<string, string?>? interactionData = null
    )
    {
        return componentId.IsEmpty || string.IsNullOrWhiteSpace(operation)
            ? Result.Failure<Interaction>(
                CmsErrors.Validation(
                    "Interaction.Invalid",
                    "InteractionComponentId and Operation are required."
                )
            )
            : Result.Success(
                new Interaction(InteractionId.Create())
                {
                    InteractionComponentId = componentId,
                    InteractionSessionId = sessionId,
                    PersonId = personId,
                    Operation = operation.Trim(),
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                    SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim(),
                    ChannelCustom1 = string.IsNullOrWhiteSpace(channelCustom1)
                        ? null
                        : channelCustom1.Trim(),
                    ChannelCustom2 = string.IsNullOrWhiteSpace(channelCustom2)
                        ? null
                        : channelCustom2.Trim(),
                    OccurredDateTimeUtc = occurredUtc.ToUniversalTime(),
                    _interactionData = interactionData is null
                        ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string?>(
                            interactionData,
                            StringComparer.OrdinalIgnoreCase
                        ),
                }
            );
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
