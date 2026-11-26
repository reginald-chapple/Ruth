namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Categorizes the functional role of a check-in thermal label or badge.</summary>
public enum CheckInLabelKind
{
    /// <summary>Wearable participant name, room, security code, and medical alert badge.</summary>
    ParticipantBadge = 0,

    /// <summary>Guardian security pickup claim receipt.</summary>
    GuardianPickupReceipt = 1,

    /// <summary>Room transfer or belongings bag tag.</summary>
    LocationTransferTag = 2,

    /// <summary>Standard visitor or conference attendee name badge.</summary>
    VisitorNameBadge = 3,

    /// <summary>Volunteer or staff role badge.</summary>
    StaffVolunteerBadge = 4,
}
