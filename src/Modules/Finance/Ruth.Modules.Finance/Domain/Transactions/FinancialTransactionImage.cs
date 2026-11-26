using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Metadata for an AES-256-GCM encrypted check or remittance image stored in RustFS.</summary>
public sealed class FinancialTransactionImage : Entity<FinancialTransactionImageId>
{
    private FinancialTransactionImage() { }

    internal FinancialTransactionImage(
        FinancialTransactionImageId id,
        FinancialTransactionId transactionId,
        CheckImageSide imageSide,
        string storageObjectKey,
        string contentType,
        long originalByteLength,
        string sha256PlaintextHex,
        string encryptionNonceBase64,
        string encryptionTagBase64,
        string keyIdentifier,
        DateTimeOffset uploadedOnUtc
    )
        : base(id)
    {
        TransactionId = transactionId;
        ImageSide = imageSide;
        StorageObjectKey = storageObjectKey;
        ContentType = contentType;
        OriginalByteLength = originalByteLength;
        Sha256PlaintextHex = sha256PlaintextHex;
        EncryptionNonceBase64 = encryptionNonceBase64;
        EncryptionTagBase64 = encryptionTagBase64;
        KeyIdentifier = keyIdentifier;
        UploadedOnUtc = uploadedOnUtc.ToUniversalTime();
    }

    public FinancialTransactionId TransactionId { get; private set; }
    public CheckImageSide ImageSide { get; private set; }
    public string StorageObjectKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "image/png";
    public long OriginalByteLength { get; private set; }
    public string Sha256PlaintextHex { get; private set; } = string.Empty;
    public string EncryptionNonceBase64 { get; private set; } = string.Empty;
    public string EncryptionTagBase64 { get; private set; } = string.Empty;
    public string KeyIdentifier { get; private set; } = string.Empty;
    public DateTimeOffset UploadedOnUtc { get; private set; }
}
