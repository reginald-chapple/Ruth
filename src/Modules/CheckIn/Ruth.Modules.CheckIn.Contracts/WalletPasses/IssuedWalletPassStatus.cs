namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Lifecycle status of an issued smart wallet pass.</summary>
public enum IssuedWalletPassStatus
{
    /// <summary>Pass is active for Kiosk barcode scanning and PassKit updates.</summary>
    Active = 0,

    /// <summary>Pass has been revoked and cannot be used for Kiosk check-in.</summary>
    Revoked = 1,

    /// <summary>Pass has reached its configured expiration timestamp.</summary>
    Expired = 2,
}
