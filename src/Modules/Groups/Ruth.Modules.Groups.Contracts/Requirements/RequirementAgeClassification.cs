namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Age classification scope for applying a GroupRequirement.</summary>
public enum RequirementAgeClassification
{
    /// <summary>Applies to all members regardless of age.</summary>
    All = 0,

    /// <summary>Applies only to adults.</summary>
    Adults = 1,

    /// <summary>Applies only to children.</summary>
    Children = 2,
}
