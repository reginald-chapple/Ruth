using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates a badge when a person has an elevated AccountProtectionProfile.</summary>
public sealed class ProtectionProfileBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "protection-profile";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Account Protection";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Person.AccountProtectionProfile == AccountProtectionProfile.Low)
        {
            return null;
        }

        var color = context.Person.AccountProtectionProfile switch
        {
            AccountProtectionProfile.Extreme => "#A4262C",
            AccountProtectionProfile.High => "#D13438",
            AccountProtectionProfile.Low => "#605E5C",
            AccountProtectionProfile.Medium => "#CA5010",
            _ => "#CA5010",
        };

        return new EvaluatedPersonBadgeResponse(
            definition.Id,
            ComponentKey,
            definition.Name,
            context.Person.AccountProtectionProfile.ToString(),
            $"Protection Level: {context.Person.AccountProtectionProfile}",
            color,
            "lock-shield",
            definition.Order
        );
    }
}
