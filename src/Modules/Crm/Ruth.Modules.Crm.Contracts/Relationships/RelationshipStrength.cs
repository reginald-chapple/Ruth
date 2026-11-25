namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Base relationship strength weight for peer-network scoring.</summary>
public enum RelationshipStrength
{
    /// <summary>No relationship weight (excluded).</summary>
    None = 0,

    /// <summary>Casual connection (weight 5).</summary>
    Casual = 5,

    /// <summary>Close connection (weight 10).</summary>
    Close = 10,

    /// <summary>Deep connection (weight 20).</summary>
    Deep = 20,
}
