namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Payment or credit instrument used for an assistance or grant disbursement.</summary>
public enum AssistanceDisbursementMethod
{
    /// <summary>Direct paper check disbursement.</summary>
    DirectCheck = 0,

    /// <summary>Electronic ACH or wire transfer.</summary>
    AchTransfer = 1,

    /// <summary>Third-party utility, landlord, or institutional vendor payment.</summary>
    VendorPayment = 2,

    /// <summary>Internal fee-waiver or scholarship credit applied directly to an open Ruth invoice.</summary>
    InternalFeeWaiverCredit = 3,
}
