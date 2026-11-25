using Ruth.Modules.Crm.Contracts.Badges;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates a badge displaying the person's formatted age and school grade.</summary>
public sealed class AgeAndBirthdayBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "age-birthday";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Age & Grade";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (
            string.IsNullOrWhiteSpace(context.Person.AgeFormatted)
            && string.IsNullOrWhiteSpace(context.Person.GradeFormatted)
        )
        {
            return null;
        }

        var parts = new[] { context.Person.AgeFormatted, context.Person.GradeFormatted }.Where(
            static s => !string.IsNullOrWhiteSpace(s)
        );
        var value = string.Join(" · ", parts);

        return new EvaluatedPersonBadgeResponse(
            definition.Id,
            ComponentKey,
            definition.Name,
            value,
            $"Age Classification: {context.Person.AgeClassification}",
            "#038387",
            "calendar",
            definition.Order
        );
    }
}
