namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Organization-agnostic categories for automated communication drip flows.</summary>
public enum CommunicationFlowCategory
{
    /// <summary>New member, constituent, or participant onboarding sequence.</summary>
    Onboarding = 0,

    /// <summary>Membership renewal, subscription cadence, or billing reminder sequence.</summary>
    MembershipAndRenewal = 1,

    /// <summary>Engagement, milestone follow-up, or re-engagement drip sequence.</summary>
    EngagementAndRetention = 2,

    /// <summary>Event, cohort, or workshop pre/post-session sequence.</summary>
    EventsAndPrograms = 3,

    /// <summary>Volunteer, staff, or operational readiness sequence.</summary>
    VolunteerAndOperations = 4,
}
