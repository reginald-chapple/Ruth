using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Grading;

/// <summary>Configurable grading system mapping percentage scores to letter/pass-fail bands.</summary>
public sealed class LearningGradingSystem
    : AggregateRoot<LearningGradingSystemId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningGradingSystem";

    private readonly List<LearningGradingScale> _scales = [];

    private LearningGradingSystem() { }

    private LearningGradingSystem(LearningGradingSystemId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<LearningGradingScale> Scales => _scales.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<LearningGradingSystem> Create(
        string name,
        IReadOnlyList<LearningGradingScaleSpec> scales,
        string? description = null
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LearningGradingSystem>(
                LmsErrors.Validation(
                    "GradingSystem.NameRequired",
                    "Grading system name is required."
                )
            );
        }

        if (scales is not { Count: > 0 })
        {
            return Result.Failure<LearningGradingSystem>(
                LmsErrors.Validation(
                    "GradingSystem.ScalesRequired",
                    "At least one grading scale band is required."
                )
            );
        }

        var system = new LearningGradingSystem(LearningGradingSystemId.Create())
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
        };

        var ordered = scales.OrderByDescending(static s => s.ThresholdPercentage).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var scaleRes = LearningGradingScale.Create(system.Id, ordered[i], i);
            if (scaleRes.IsFailure)
            {
                return Result.Failure<LearningGradingSystem>(scaleRes.PrimaryError);
            }

            system._scales.Add(scaleRes.Value);
        }

        return Result.Success(system);
    }

    public LearningGradingScale? EvaluateScale(decimal percentage)
    {
        var clamped = Math.Clamp(percentage, 0m, 100m);
        var descending = _scales.OrderByDescending(static s => s.ThresholdPercentage).ToList();
        return descending.Find(s => clamped >= s.ThresholdPercentage) ?? descending.LastOrDefault();
    }
}
