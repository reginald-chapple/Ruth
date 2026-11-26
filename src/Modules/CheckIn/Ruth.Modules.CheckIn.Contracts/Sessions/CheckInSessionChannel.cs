namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Identifies the operational channel that initiated a check-in session.</summary>
public enum CheckInSessionChannel
{
    /// <summary>Touchscreen or self-service physical kiosk station.</summary>
    Kiosk = 0,

    /// <summary>Constituent mobile pre-check-in or express pass API.</summary>
    Mobile = 1,

    /// <summary>Staff or room-manager tablet station with override privileges.</summary>
    StaffOverride = 2,
}
