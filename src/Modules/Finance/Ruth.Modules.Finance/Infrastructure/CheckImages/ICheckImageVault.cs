using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Infrastructure.CheckImages;

internal interface ICheckImageVault
{
    Task<
        Result<(
            string StorageObjectKey,
            long ByteLength,
            string Sha256Hex,
            string NonceBase64,
            string TagBase64,
            string KeyId
        )>
    > EncryptAndUploadAsync(
        FinancialTransactionId transactionId,
        FinancialTransactionImageId imageId,
        CheckImageSide side,
        ReadOnlyMemory<byte> plaintextBytes,
        CancellationToken cancellationToken = default
    );

    Task<Result<byte[]>> DownloadAndDecryptAsync(
        FinancialTransactionImage metadata,
        CancellationToken cancellationToken = default
    );
}
