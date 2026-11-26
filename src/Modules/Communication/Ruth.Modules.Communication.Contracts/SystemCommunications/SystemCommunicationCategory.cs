namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Functional category grouping transactional system communications across modules.</summary>
public enum SystemCommunicationCategory
{
    /// <summary>Authentication, onboarding, password reset, and account confirmation alerts.</summary>
    IdentityAndSecurity = 0,

    /// <summary>Group membership, shift scheduling, RSVP, and attendance notifications.</summary>
    GroupsAndScheduling = 1,

    /// <summary>Check-in receipts, guardian pickup alerts, and room safety notifications.</summary>
    CheckInAndSafety = 2,

    /// <summary>Payment receipts, dues/invoice confirmations, and tax-deductible gift acknowledgments.</summary>
    FinanceAndBilling = 3,

    /// <summary>Workflow task assignments, approvals, and operational escalations.</summary>
    WorkflowAndOperations = 4,

    /// <summary>General organization-wide transactional notifications.</summary>
    General = 5,
}
