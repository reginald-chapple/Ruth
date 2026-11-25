namespace Ruth.SharedKernel.Security;

/// <summary>Ultimate default root authority where View and Tag default to allowed.</summary>
public sealed class GlobalDefaultSecured : ISecured
{
    /// <summary>Canonical entity type key for GlobalDefault.</summary>
    public const string TypeKey = "Ruth.Security.GlobalDefault";

    /// <summary>Deterministic SystemGuid for GlobalDefault.</summary>
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
        0x01
    );

    /// <summary>Gets the singleton instance of <see cref="GlobalDefaultSecured" />.</summary>
    public static GlobalDefaultSecured Instance { get; } = new();

    private GlobalDefaultSecured() { }

    /// <inheritdoc />
    public string EntityTypeKey => TypeKey;

    /// <inheritdoc />
    public Guid EntityId => Guid.Empty;

    /// <inheritdoc />
    public ISecured? ParentAuthority => null;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return string.Equals(action, AuthorizationActions.View, StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, AuthorizationActions.Tag, StringComparison.OrdinalIgnoreCase);
    }
}
