namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Read metadata for an AES-256-GCM encrypted check or remittance image stored in RustFS.</summary>
/// <param name="Id">Transaction image identifier.</param>
/// <param name="ImageSide">Front, Back, or RemittanceSlip.</param>
/// <param name="ContentType">Original MIME content type.</param>
/// <param name="OriginalByteLength">Plaintext image byte length.</param>
/// <param name="Sha256PlaintextHex">SHA-256 integrity digest of the plaintext image.</param>
/// <param name="KeyIdentifier">Encryption key identifier.</param>
/// <param name="UploadedOnUtc">UTC timestamp when encrypted and stored.</param>
public sealed record FinancialTransactionImageResponse(
    Guid Id,
    CheckImageSide ImageSide,
    string ContentType,
    long OriginalByteLength,
    string Sha256PlaintextHex,
    string KeyIdentifier,
    DateTimeOffset UploadedOnUtc
);
