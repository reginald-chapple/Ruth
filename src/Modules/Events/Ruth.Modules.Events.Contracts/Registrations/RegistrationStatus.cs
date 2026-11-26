namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Lifecycle state of a Registration submission.</summary>
public enum RegistrationStatus
{
    /// <summary>Active registration with one or more non-cancelled registrants.</summary>
    Active = 0,

    /// <summary>Cancelled registration.</summary>
    Cancelled = 1,
}
