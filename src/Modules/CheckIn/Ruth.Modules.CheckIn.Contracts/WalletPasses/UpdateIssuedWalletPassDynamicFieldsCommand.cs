using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Updates dynamic fields on an issued smart wallet pass and triggers PassKit push notifications to registered devices.</summary>
/// <param name="IssuedPassId">Target issued wallet pass identifier.</param>
/// <param name="DynamicFields">Updated tier, punch/credit balance, shift, or event fields.</param>
/// <param name="ExpiresAtUtc">Optional updated expiration timestamp in UTC.</param>
public sealed record UpdateIssuedWalletPassDynamicFieldsCommand(
    Guid IssuedPassId,
    WalletPassDynamicFieldsDto DynamicFields,
    DateTimeOffset? ExpiresAtUtc = null
) : ICommand<IssuedWalletPassResponse>;
