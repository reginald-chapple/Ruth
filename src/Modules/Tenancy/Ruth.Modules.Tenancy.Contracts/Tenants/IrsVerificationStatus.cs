namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Outcome of checking an organization's EIN against the IRS Exempt Organizations Business Master File.</summary>
public enum IrsVerificationStatus
{
    /// <summary>No IRS EO BMF check has been executed yet.</summary>
    Unchecked = 0,

    /// <summary>Confirmed active tax-exempt status in IRS EO BMF / Publication 78.</summary>
    VerifiedActiveExempt = 1,

    /// <summary>Exempt status was revoked or organization subsection is ineligible.</summary>
    RevokedOrIneligible = 2,

    /// <summary>EIN was not found in the current IRS EO BMF extract.</summary>
    NotFoundInEoBmf = 3,

    /// <summary>Newly issued determination letter requiring manual operator verification.</summary>
    ManualReviewRequired = 4,
}
