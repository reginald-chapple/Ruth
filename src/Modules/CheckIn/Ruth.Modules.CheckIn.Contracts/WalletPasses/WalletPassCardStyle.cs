namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Organization-neutral visual and semantic card styles for issued smart wallet passes.</summary>
public enum WalletPassCardStyle
{
    /// <summary>Member credential and facility access card showing tier and status.</summary>
    MemberCredential = 0,

    /// <summary>Multi-visit punch pass or prepaid credit balance card.</summary>
    PunchOrCreditCard = 1,

    /// <summary>Scheduled volunteer, facilitator, or staff shift credential.</summary>
    VolunteerOrStaffShift = 2,

    /// <summary>Event, workshop, or conference admission ticket.</summary>
    EventTicket = 3,
}
