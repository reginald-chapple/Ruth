using Ruth.Modules.Crm.Contracts.Badges;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates the person's highest-priority active signal badge.</summary>
public sealed class TopSignalBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "top-signal";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Top Person Signal";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        return !context.Person.TopSignalId.HasValue
            ? null
            : new EvaluatedPersonBadgeResponse(
                definition.Id,
                ComponentKey,
                definition.Name,
                "Active Signal",
                "Person has an active high-priority signal",
                context.Person.TopSignalColor ?? "#D13438",
                context.Person.TopSignalIcon ?? "flag",
                definition.Order
            );
    }
}
