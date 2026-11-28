using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W8S4CheckImageTenantAadTests
{
    private static readonly TenantId _tenantA = TenantId.Create();

    private static readonly TenantId _tenantB = TenantId.Create();

    private static readonly byte[] _samplePng =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D,
        0x49,
        0x48,
        0x44,
        0x52,
    ];

    [Fact]
    public async Task AesGcmCheckImageVault_BindsTenantIdIntoAad_AndRejectsCrossTenantDecryption()
    {
        var storage = new InMemoryStorageProvider();
        var vault = new AesGcmCheckImageVault(
            storage,
            Options.Create(new FinanceSecurityOptions())
        );

        var txId = FinancialTransactionId.Create();
        var imageId = FinancialTransactionImageId.Create();
        FinancialTransactionImage metadata;

        using (AmbientTenantContext.BeginScope(_tenantA))
        {
            var encrypted = await vault.EncryptAndUploadAsync(
                txId,
                imageId,
                CheckImageSide.Front,
                _samplePng
            );
            Assert.True(encrypted.IsSuccess);
            Assert.StartsWith(
                $"tenants/{_tenantA.Value:D}/",
                encrypted.Value.StorageObjectKey,
                StringComparison.Ordinal
            );

            metadata = new FinancialTransactionImage(
                imageId,
                txId,
                CheckImageSide.Front,
                encrypted.Value.StorageObjectKey,
                "image/png",
                encrypted.Value.ByteLength,
                encrypted.Value.Sha256Hex,
                encrypted.Value.NonceBase64,
                encrypted.Value.TagBase64,
                encrypted.Value.KeyId,
                DateTimeOffset.UtcNow
            );

            var decryptedA = await vault.DownloadAndDecryptAsync(metadata);
            Assert.True(decryptedA.IsSuccess);
            Assert.Equal(_samplePng, decryptedA.Value);
        }

        using (AmbientTenantContext.BeginScope(_tenantB))
        {
            var crossTenantAttempt = await vault.DownloadAndDecryptAsync(metadata);
            Assert.True(crossTenantAttempt.IsFailure);
        }
    }
}
