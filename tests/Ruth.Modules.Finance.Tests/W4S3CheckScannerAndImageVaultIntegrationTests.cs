using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S3CheckScannerAndImageVaultIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task CheckScannerIngest_AutoMatchesMicrHash_AndEncryptsCheckImagesWithTamperDetection()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var storage = scope.ServiceProvider.GetRequiredService<InMemoryStorageProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var accountId = (
            await queries.QueryAsync<
                GetFinancialAccountsQuery,
                IReadOnlyList<FinancialAccountResponse>
            >(new GetFinancialAccountsQuery())
        )
            .Value[0]
            .Id;

        var donorId = Guid.CreateVersion7();
        var samplePngBytes = Encoding.UTF8.GetBytes("PNG-FAKE-CHECK-IMAGE-PAYLOAD-FRONT-2026");

        var firstTxId = await IngestAndVerifyCheckMatchingAsync(
            commands,
            accountId,
            donorId,
            samplePngBytes
        );
        await VerifyAesGcmEncryptionAndTamperRejectionAsync(
            queries,
            dbContext,
            storage,
            firstTxId,
            samplePngBytes
        );
    }

    private static async Task<Guid> IngestAndVerifyCheckMatchingAsync(
        ICommandDispatcher commands,
        Guid accountId,
        Guid donorId,
        byte[] samplePngBytes
    )
    {
        var firstScan = await commands.SendAsync<
            IngestScannedCheckCommand,
            CheckScannerIngestResponse
        >(
            new IngestScannedCheckCommand(
                "021000021",
                "7788990011",
                "1001",
                125.00m,
                "USD",
                accountId,
                OverridePersonId: donorId,
                BindBankIdentifierToPerson: true,
                FrontImageBase64: Convert.ToBase64String(samplePngBytes),
                ImageContentType: "image/png"
            )
        );
        Assert.True(firstScan.IsSuccess);
        Assert.Equal(donorId, firstScan.Value.MatchedPersonId);
        _ = Assert.Single(firstScan.Value.Transaction.Images);

        var secondScan = await commands.SendAsync<
            IngestScannedCheckCommand,
            CheckScannerIngestResponse
        >(
            new IngestScannedCheckCommand(
                "0210-0002-1",
                "7788-990011",
                "1002",
                200.00m,
                "USD",
                accountId,
                OverridePersonId: null
            )
        );
        Assert.True(secondScan.IsSuccess);
        Assert.Equal(donorId, secondScan.Value.MatchedPersonId);

        return firstScan.Value.Transaction.Id;
    }

    private static async Task VerifyAesGcmEncryptionAndTamperRejectionAsync(
        IQueryDispatcher queries,
        FinanceDbContext dbContext,
        InMemoryStorageProvider storage,
        Guid transactionId,
        byte[] expectedPlaintext
    )
    {
        var txStrongId = FinancialTransactionId.From(transactionId);
        var imageMeta = await dbContext.FinancialTransactionImages.FirstAsync(i =>
            i.TransactionId == txStrongId
        );
        var storedCiphertext = storage.GetRawBytes(imageMeta.StorageObjectKey);
        Assert.False(storedCiphertext.AsSpan().SequenceEqual(expectedPlaintext));

        var decrypted = await queries.QueryAsync<
            DownloadDecryptedCheckImageQuery,
            DecryptedCheckImageResponse
        >(new DownloadDecryptedCheckImageQuery(transactionId, imageMeta.Id.Value));
        Assert.True(decrypted.IsSuccess);
        Assert.Equal(expectedPlaintext, decrypted.Value.PlaintextBytes);

        storage.TamperByte(imageMeta.StorageObjectKey, 0);
        var tampered = await queries.QueryAsync<
            DownloadDecryptedCheckImageQuery,
            DecryptedCheckImageResponse
        >(new DownloadDecryptedCheckImageQuery(transactionId, imageMeta.Id.Value));
        Assert.True(tampered.IsFailure);
    }
}
