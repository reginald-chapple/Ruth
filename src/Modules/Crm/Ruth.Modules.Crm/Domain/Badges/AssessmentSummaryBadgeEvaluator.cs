using System.Globalization;
using Ruth.Modules.Crm.Contracts.Badges;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Evaluates a badge summarizing a person's completed assessments (DISC, Team Strengths, etc.).</summary>
public sealed class AssessmentSummaryBadgeEvaluator : IPersonBadgeEvaluator
{
    /// <summary>Unique badge component key.</summary>
    public const string Key = "assessment-summary";

    /// <inheritdoc />
    public string ComponentKey => Key;

    /// <inheritdoc />
    public string DisplayName => "Assessments";

    /// <inheritdoc />
    public EvaluatedPersonBadgeResponse? Evaluate(
        BadgeDefinitionResponse definition,
        PersonBadgeEvaluationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (context.CompletedAssessmentsCount <= 0)
        {
            return null;
        }

        var value = !string.IsNullOrWhiteSpace(context.LatestAssessmentSummary)
            ? context.LatestAssessmentSummary
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{context.CompletedAssessmentsCount} Completed"
            );

        return new EvaluatedPersonBadgeResponse(
            definition.Id,
            ComponentKey,
            definition.Name,
            value,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Completed Assessments: {context.CompletedAssessmentsCount}"
            ),
            "#4F6BED",
            "clipboard-checkmark",
            definition.Order
        );
    }
}
