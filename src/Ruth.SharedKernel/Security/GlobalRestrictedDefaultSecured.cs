namespace Ruth.SharedKernel.Security;

/// <summary>Restricted root authority where every action defaults to denied unless explicitly allowed.</summary>
public sealed class GlobalRestrictedDefaultSecured : ISecured
{
    /// <summary>Canonical entity type key for GlobalRestrictedDefault.</summary>
    public const string TypeKey = "Ruth.Security.GlobalRestrictedDefault";

    /// <summary>Deterministic SystemGuid for GlobalRestrictedDefault.</summary>
    public static readonly Guid SystemGuid = new(
        0x34d56301,
        0x79b0,
        0x4c9e,
        0x9a,
        0x17,
        0x2a,
        0x1e,
        0x5a,
        0x4a,
        0x00,
        0x02
    );

    /// <summary>Gets the singleton instance of <see cref="GlobalRestrictedDefaultSecured" />.</summary>
    public static GlobalRestrictedDefaultSecured Instance { get; } = new();

    private GlobalRestrictedDefaultSecured() { }

    /// <inheritdoc />
    public string EntityTypeKey => TypeKey;

    /// <inheritdoc />
    public Guid EntityId => Guid.Empty;

    /// <inheritdoc />
    public ISecured? ParentAuthority => null;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return false;
    }
}
