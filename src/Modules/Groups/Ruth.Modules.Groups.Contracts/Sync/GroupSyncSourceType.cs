namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Authoritative person set source used by GroupSync reconciliation.</summary>
public enum GroupSyncSourceType
{
    /// <summary>Synchronize from a Core EntitySet containing Person IDs.</summary>
    EntitySet = 0,

    /// <summary>Synchronize from CRM people matching a specific ConnectionStatus.</summary>
    CrmConnectionStatus = 1,

    /// <summary>Synchronize from active members of another Group.</summary>
    GroupMembers = 2,
}
