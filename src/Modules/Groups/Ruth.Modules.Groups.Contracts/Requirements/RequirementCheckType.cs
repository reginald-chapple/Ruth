namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>How a GroupRequirementType is evaluated for a person.</summary>
public enum RequirementCheckType
{
    /// <summary>Manually checked off by a leader or administrator.</summary>
    Manual = 0,

    /// <summary>Requires a clear CRM BackgroundCheck (optionally within a maximum age in days).</summary>
    BackgroundCheckClear = 1,

    /// <summary>Requires a matching CRM Person JSONB attribute value.</summary>
    PersonAttribute = 2,

    /// <summary>Requires active membership in a prerequisite Group.</summary>
    GroupMembership = 3,
}
