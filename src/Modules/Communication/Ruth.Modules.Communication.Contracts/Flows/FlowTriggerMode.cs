namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Enrollment trigger modes for a CommunicationFlow.</summary>
public enum FlowTriggerMode
{
    /// <summary>Staff or API launches explicit cohort instances on demand.</summary>
    OnDemandCohort = 0,

    /// <summary>Recurring scheduled cohorts launch on a cadence.</summary>
    RecurringCohort = 1,

    /// <summary>Rolling individual enrollment triggered continuously as people qualify.</summary>
    RollingEnrollment = 2,
}
