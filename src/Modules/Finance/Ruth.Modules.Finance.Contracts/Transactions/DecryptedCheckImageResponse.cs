namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Authenticated, decrypted check or remittance image payload retrieved from RustFS.</summary>
/// <param name="ImageId">Transaction image identifier.</param>
/// <param name="TransactionId">Parent financial transaction identifier.</param>
/// <param name="ImageSide">Front, Back, or RemittanceSlip.</param>
/// <param name="ContentType">MIME content type.</param>
/// <param name="PlaintextBytes">Authenticated decrypted image bytes.</param>
public sealed record DecryptedCheckImageResponse(
    Guid ImageId,
    Guid TransactionId,
    CheckImageSide ImageSide,
    string ContentType,
    byte[] PlaintextBytes
);
