namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Direction of peer-network relationship score movement since the prior calculation.</summary>
public enum RelationshipTrend
{
    /// <summary>Score remained unchanged.</summary>
    Level = 0,

    /// <summary>Score decreased (decaying).</summary>
    Down = 1,

    /// <summary>Score increased (growing).</summary>
    Up = 2,
}
