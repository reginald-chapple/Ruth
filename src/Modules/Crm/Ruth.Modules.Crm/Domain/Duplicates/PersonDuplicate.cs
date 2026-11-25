using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Canonical scored potential duplicate pair (always ordered Person1Id &lt; Person2Id).</summary>
public sealed class PersonDuplicate : AggregateRoot<PersonDuplicateId>, IAuditableEntity
{
    private PersonDuplicate() { }

    private PersonDuplicate(
        PersonDuplicateId id,
        PersonId person1Id,
        PersonId person2Id,
        DuplicateScoreEvaluation evaluation,
        DateTimeOffset scannedOnUtc
    )
        : base(id)
    {
        Person1Id = person1Id;
        Person2Id = person2Id;
        Score = evaluation.Score;
        Capacity = evaluation.Capacity;
        TotalCapacity = evaluation.TotalCapacity;
        ConfidenceScore = evaluation.ConfidenceScore;
        ScoreDetail = evaluation.ScoreDetail;
        LastScannedOnUtc = scannedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the lexicographically smaller person identifier in the pair.</summary>
    public PersonId Person1Id { get; private set; }

    /// <summary>Gets the lexicographically larger person identifier in the pair.</summary>
    public PersonId Person2Id { get; private set; }

    /// <summary>Gets the raw weighted duplicate score.</summary>
    public int Score { get; private set; }

    /// <summary>Gets the populated field capacity of the pair.</summary>
    public int Capacity { get; private set; }

    /// <summary>Gets the maximum possible linkage weight (100).</summary>
    public int TotalCapacity { get; private set; }

    /// <summary>Gets the completeness-adjusted confidence percentage (0–100).</summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>Gets the pipe-delimited breakdown of matching criteria.</summary>
    public string ScoreDetail { get; private set; } = string.Empty;

    /// <summary>Gets whether staff permanently confirmed this pair is not a duplicate.</summary>
    public bool IsConfirmedAsNotDuplicate { get; private set; }

    /// <summary>Gets whether this pair is ignored until its confidence score changes.</summary>
    public bool IgnoreUntilScoreChanges { get; private set; }

    /// <summary>Gets the UTC timestamp of the most recent duplicate scan.</summary>
    public DateTimeOffset LastScannedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Orders a pair of PersonIds canonically so each pair is stored once.</summary>
    public static (PersonId First, PersonId Second) OrderPair(PersonId a, PersonId b)
    {
        return a.Value.CompareTo(b.Value) <= 0 ? (a, b) : (b, a);
    }

    /// <summary>Creates a new scored duplicate pair.</summary>
    public static PersonDuplicate Create(
        PersonId a,
        PersonId b,
        DuplicateScoreEvaluation evaluation,
        DateTimeOffset scannedOnUtc
    )
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        var (first, second) = OrderPair(a, b);
        return new PersonDuplicate(
            PersonDuplicateId.Create(),
            first,
            second,
            evaluation,
            scannedOnUtc
        );
    }

    /// <summary>Updates scores from a new scan, automatically clearing IgnoreUntilScoreChanges if ConfidenceScore drifted.</summary>
    public void UpdateFromScan(DuplicateScoreEvaluation evaluation, DateTimeOffset scannedOnUtc)
    {
        ArgumentNullException.ThrowIfNull(evaluation);

        var scoreChanged = ConfidenceScore != evaluation.ConfidenceScore;
        Score = evaluation.Score;
        Capacity = evaluation.Capacity;
        TotalCapacity = evaluation.TotalCapacity;
        ConfidenceScore = evaluation.ConfidenceScore;
        ScoreDetail = evaluation.ScoreDetail;
        LastScannedOnUtc = scannedOnUtc.ToUniversalTime();

        if (scoreChanged && IgnoreUntilScoreChanges)
        {
            IgnoreUntilScoreChanges = false;
        }
    }

    /// <summary>Sets staff review flags for this duplicate pair.</summary>
    public void SetReviewStatus(bool isConfirmedAsNotDuplicate, bool ignoreUntilScoreChanges)
    {
        IsConfirmedAsNotDuplicate = isConfirmedAsNotDuplicate;
        IgnoreUntilScoreChanges = ignoreUntilScoreChanges;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
