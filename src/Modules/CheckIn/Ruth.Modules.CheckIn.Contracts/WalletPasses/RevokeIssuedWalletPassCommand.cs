using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Revokes an issued smart wallet pass so Kiosk scans reject it and registered devices receive an update.</summary>
/// <param name="IssuedPassId">Target issued wallet pass identifier.</param>
/// <param name="Reason">Optional audit reason for revocation.</param>
public sealed record RevokeIssuedWalletPassCommand(Guid IssuedPassId, string? Reason = null)
    : ICommand<IssuedWalletPassResponse>;
