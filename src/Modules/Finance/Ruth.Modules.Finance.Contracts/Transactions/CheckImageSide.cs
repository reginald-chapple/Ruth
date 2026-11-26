namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Identifies the face or slip of an encrypted check/remittance image.</summary>
public enum CheckImageSide
{
    /// <summary>Front face of the check.</summary>
    Front = 0,

    /// <summary>Endorsed back face of the check.</summary>
    Back = 1,

    /// <summary>Accompanying remittance or payment coupon slip.</summary>
    RemittanceSlip = 2,
}
