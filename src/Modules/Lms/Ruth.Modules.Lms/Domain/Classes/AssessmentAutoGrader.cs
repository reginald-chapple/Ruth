using System.Text.Json;
using Ruth.Modules.Lms.Contracts.Classes;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>
/// Clean-room auto-grading engine for LMS activities:
/// - Video, Article, Acknowledgment: automatically awarded full points on submission.
/// - Assessment: auto-graded when an objective answer key exists in ActivityConfigJson; otherwise queued for facilitator.
/// - FileUpload: always queued for facilitator review (RequiresGrading = true).
/// </summary>
public static class AssessmentAutoGrader
{
    public sealed record EvaluationOutcome(
        bool RequiresGrading,
        int PointsEarned,
        bool IsFacilitatorCompleted
    );

    public static EvaluationOutcome EvaluateSubmission(
        LearningActivity activity,
        string? submissionDataJson
    )
    {
        ArgumentNullException.ThrowIfNull(activity);

        return activity.ComponentType switch
        {
            LearningActivityComponentType.Video
            or LearningActivityComponentType.Article
            or LearningActivityComponentType.Acknowledgment => new EvaluationOutcome(
                RequiresGrading: false,
                PointsEarned: activity.Points,
                IsFacilitatorCompleted: true
            ),
            LearningActivityComponentType.FileUpload => new EvaluationOutcome(
                RequiresGrading: true,
                PointsEarned: 0,
                IsFacilitatorCompleted: false
            ),
            LearningActivityComponentType.Assessment => EvaluateAssessment(
                activity,
                submissionDataJson
            ),
            _ => new EvaluationOutcome(
                RequiresGrading: true,
                PointsEarned: 0,
                IsFacilitatorCompleted: false
            ),
        };
    }

    private static EvaluationOutcome EvaluateAssessment(
        LearningActivity activity,
        string? submissionDataJson
    )
    {
        var answerKey = ParseStringMap(activity.ActivityConfigJson, "answerKey");
        if (answerKey is null || answerKey.Count == 0)
        {
            return new EvaluationOutcome(
                RequiresGrading: true,
                PointsEarned: 0,
                IsFacilitatorCompleted: false
            );
        }

        var answers = ParseStringMap(submissionDataJson, "answers")
            ?? ParseRootStringMap(submissionDataJson)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var correct = 0;
        foreach (var (questionKey, expectedAnswer) in answerKey)
        {
            if (
                answers.TryGetValue(questionKey, out var actual)
                && string.Equals(
                    actual.Trim(),
                    expectedAnswer.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                correct++;
            }
        }

        var ratio = (decimal)correct / answerKey.Count;
        var earned = (int)Math.Round(activity.Points * ratio, 0, MidpointRounding.AwayFromZero);
        return new EvaluationOutcome(
            RequiresGrading: false,
            PointsEarned: earned,
            IsFacilitatorCompleted: true
        );
    }

    private static Dictionary<string, string>? ParseStringMap(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return
                doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty(propertyName, out var sub)
                || sub.ValueKind != JsonValueKind.Object
                ? null
                : ReadObjectStrings(sub);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string>? ParseRootStringMap(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                ? ReadObjectStrings(doc.RootElement)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ReadObjectStrings(JsonElement obj)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            map[prop.Name] = prop.Value.ToString();
        }

        return map;
    }
}
