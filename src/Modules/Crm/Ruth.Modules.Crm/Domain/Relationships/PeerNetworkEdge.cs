using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Directed scored peer-network connection synchronized into PostgreSQL Apache AGE.</summary>
public sealed class PeerNetworkEdge : AggregateRoot<PeerNetworkEdgeId>, IAuditableEntity
{
    private PeerNetworkEdge() { }

    private PeerNetworkEdge(
        PeerNetworkEdgeId id,
        PersonId sourcePersonId,
        PersonId targetPersonId,
        PeerNetworkRelationshipType relationshipType,
        Guid? relatedEntityId,
        Guid? classificationEntityId,
        RelationshipStrength strength,
        DateOnly startDate,
        DateOnly? endDate,
        decimal score,
        RelationshipTrend trend,
        string caption,
        DateTimeOffset nowUtc
    )
        : base(id)
    {
        SourcePersonId = sourcePersonId;
        TargetPersonId = targetPersonId;
        RelationshipType = relationshipType;
        RelatedEntityId = relatedEntityId;
        ClassificationEntityId = classificationEntityId;
        Strength = strength;
        RelationshipStartDate = startDate;
        RelationshipEndDate = endDate;
        RelationshipScore = score;
        Trend = trend;
        Caption = caption;
        LastUpdatedOnUtc = nowUtc.ToUniversalTime();
    }

    /// <summary>Gets the source person identifier.</summary>
    public PersonId SourcePersonId { get; private set; }

    /// <summary>Gets the target person identifier.</summary>
    public PersonId TargetPersonId { get; private set; }

    /// <summary>Gets the peer-network relationship category.</summary>
    public PeerNetworkRelationshipType RelationshipType { get; private set; }

    /// <summary>Gets the optional originating entity identifier (e.g., Group or Household).</summary>
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>Gets the optional classification entity identifier (e.g., GroupType).</summary>
    public Guid? ClassificationEntityId { get; private set; }

    /// <summary>Gets the base relationship strength.</summary>
    public RelationshipStrength Strength { get; private set; }

    /// <summary>Gets the date the connection began.</summary>
    public DateOnly RelationshipStartDate { get; private set; }

    /// <summary>Gets the optional date the connection ended (triggers decay).</summary>
    public DateOnly? RelationshipEndDate { get; private set; }

    /// <summary>Gets the computed relationship score.</summary>
    public decimal RelationshipScore { get; private set; }

    /// <summary>Gets the previous score used to calculate trend.</summary>
    public decimal? PreviousScore { get; private set; }

    /// <summary>Gets the direction of score movement.</summary>
    public RelationshipTrend Trend { get; private set; }

    /// <summary>Gets the human-readable caption for this edge.</summary>
    public string Caption { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp of the last score recalculation.</summary>
    public DateTimeOffset LastUpdatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a scored peer-network edge.</summary>
    public static Result<PeerNetworkEdge> Create(
        PersonId sourcePersonId,
        PersonId targetPersonId,
        PeerNetworkRelationshipType relationshipType,
        RelationshipStrength strength,
        string caption,
        DateOnly startDate,
        DateOnly? endDate,
        DateTimeOffset nowUtc,
        Guid? relatedEntityId = null,
        Guid? classificationEntityId = null,
        decimal rolePairMultiplier = 1.0m,
        bool isGrowthEnabled = true
    )
    {
        if (sourcePersonId == targetPersonId)
        {
            return CrmErrors.RelationshipSelfReference;
        }

        var captionResult = CrmTextPolicy.ValidateRequiredText(
            caption,
            200,
            CrmErrors.TextTooLong,
            CrmErrors.TextTooLong
        );
        if (captionResult.IsFailure)
        {
            return Result.Failure<PeerNetworkEdge>(captionResult.Errors);
        }

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var (score, trend) = PeerNetworkScoreCalculator.Calculate(
            strength,
            startDate,
            endDate,
            today,
            rolePairMultiplier,
            isGrowthEnabled
        );

        return Result.Success(
            new PeerNetworkEdge(
                PeerNetworkEdgeId.Create(),
                sourcePersonId,
                targetPersonId,
                relationshipType,
                relatedEntityId,
                classificationEntityId,
                strength,
                startDate,
                endDate,
                score,
                trend,
                captionResult.Value,
                nowUtc
            )
        );
    }

    /// <summary>Recalculates the edge score and trend.</summary>
    public void Recalculate(
        RelationshipStrength strength,
        DateOnly startDate,
        DateOnly? endDate,
        string caption,
        DateTimeOffset nowUtc,
        decimal rolePairMultiplier = 1.0m,
        bool isGrowthEnabled = true
    )
    {
        PreviousScore = RelationshipScore;
        Strength = strength;
        RelationshipStartDate = startDate;
        RelationshipEndDate = endDate;
        Caption = caption.Trim();

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var (score, trend) = PeerNetworkScoreCalculator.Calculate(
            strength,
            startDate,
            endDate,
            today,
            rolePairMultiplier,
            isGrowthEnabled,
            PreviousScore
        );
        RelationshipScore = score;
        Trend = trend;
        LastUpdatedOnUtc = nowUtc.ToUniversalTime();
    }

    internal void RepointSource(PersonId target)
    {
        SourcePersonId = target;
    }

    internal void RepointTarget(PersonId target)
    {
        TargetPersonId = target;
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
