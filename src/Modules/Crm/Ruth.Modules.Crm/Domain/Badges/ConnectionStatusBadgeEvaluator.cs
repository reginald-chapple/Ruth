using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates a badge displaying the person's connection status.</summary>
public sealed class ConnectionStatusBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "connection-status";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Connection Status";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        var status = context.Person.ConnectionStatus;
        var color = status switch
        {
            PersonConnectionStatus.Member => "#107C10",
            PersonConnectionStatus.Attendee => "#0078D4",
            PersonConnectionStatus.Participant => "#8764B8",
            PersonConnectionStatus.Visitor => "#CA5010",
            PersonConnectionStatus.Prospect => "#605E5C",
            _ => "#605E5C",
        };

        return new EvaluatedPersonBadgeResponse(
            definition.Id,
            ComponentKey,
            definition.Name,
            status.ToString(),
            $"Connection Status: {status}",
            color,
            "person-ribbon",
            definition.Order
        );
    }
}
