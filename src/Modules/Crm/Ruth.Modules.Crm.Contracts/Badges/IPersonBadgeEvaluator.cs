namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Modular DI-registered contract for evaluating a person profile badge.</summary>
public interface IPersonBadgeEvaluator
{
    /// <summary>Gets the unique component key identifying this badge evaluator.</summary>
    string ComponentKey { get; }

    /// <summary>Gets the human-readable display name of this badge component.</summary>
    string DisplayName { get; }

    /// <summary>Evaluates whether this badge should render for the given person context.</summary>
    EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    );
}
