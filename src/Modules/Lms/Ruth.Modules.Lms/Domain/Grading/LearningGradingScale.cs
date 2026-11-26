using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Grading;

/// <summary>Grade band threshold inside a LearningGradingSystem.</summary>
public sealed class LearningGradingScale : Entity<LearningGradingScaleId>
{
    private LearningGradingScale() { }

    private LearningGradingScale(LearningGradingScaleId id)
        : base(id) { }

    public LearningGradingSystemId LearningGradingSystemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal ThresholdPercentage { get; private set; }
    public bool IsPassing { get; private set; } = true;
    public string? HighlightColor { get; private set; }
    public string? Description { get; private set; }
    public int Order { get; private set; }

    public static Result<LearningGradingScale> Create(
        LearningGradingSystemId systemId,
        LearningGradingScaleSpec spec,
        int order
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        return string.IsNullOrWhiteSpace(spec.Name)
                ? Result.Failure<LearningGradingScale>(
                    LmsErrors.Validation(
                        "GradingScale.NameRequired",
                        "Grade scale name is required."
                    )
                )
            : spec.ThresholdPercentage is < 0m or > 100m
                ? Result.Failure<LearningGradingScale>(
                    LmsErrors.Validation(
                        "GradingScale.ThresholdOutOfRange",
                        "Threshold percentage must be between 0 and 100."
                    )
                )
            : Result.Success(
                new LearningGradingScale(LearningGradingScaleId.Create())
                {
                    LearningGradingSystemId = systemId,
                    Name = spec.Name.Trim(),
                    ThresholdPercentage = Math.Round(
                        spec.ThresholdPercentage,
                        2,
                        MidpointRounding.AwayFromZero
                    ),
                    IsPassing = spec.IsPassing,
                    HighlightColor = string.IsNullOrWhiteSpace(spec.HighlightColor)
                        ? null
                        : spec.HighlightColor.Trim(),
                    Description = string.IsNullOrWhiteSpace(spec.Description)
                        ? null
                        : spec.Description.Trim(),
                    Order = order,
                }
            );
    }
}
