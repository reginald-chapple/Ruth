using Ruth.Modules.Crm.Contracts.Badges;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates a badge reflecting the person's most recent background check status.</summary>
public sealed class BackgroundCheckStatusBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "background-check";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Background Check";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.LatestBackgroundCheckStatus))
        {
            return null;
        }

        var (label, color) = context.LatestBackgroundCheckRecordFound switch
        {
            false => ("Pass", "#107C10"),
            true => ("Review", "#D13438"),
            null => ("Pending", "#CA5010"),
        };

        return new EvaluatedPersonBadgeResponse(
            definition.Id,
            ComponentKey,
            definition.Name,
            label,
            $"Latest Background Check: {context.LatestBackgroundCheckStatus}",
            color,
            "shield-checkmark",
            definition.Order
        );
    }
}
