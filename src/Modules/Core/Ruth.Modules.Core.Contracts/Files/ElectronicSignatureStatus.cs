namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lifecycle state of an electronic signature document.</summary>
public enum ElectronicSignatureStatus
{
    /// <summary>Draft or not yet sent.</summary>
    None = 0,

    /// <summary>Invitation sent and awaiting signature.</summary>
    Sent = 1,

    /// <summary>Signed and cryptographically sealed.</summary>
    Completed = 2,

    /// <summary>Cancelled prior to completion.</summary>
    Cancelled = 3,

    /// <summary>Expired before completion or past validity window.</summary>
    Expired = 4,
}
