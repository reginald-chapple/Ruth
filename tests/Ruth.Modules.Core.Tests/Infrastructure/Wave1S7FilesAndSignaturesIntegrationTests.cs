using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Tests.Domain;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class Wave1S7FilesAndSignaturesIntegrationTests(
    CorePostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task RustFs_Upload_PresignedUrl_And_ImageCrop_Work_Against_Live_RustFs()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(
            cs,
            rustFsEndpoint: postgresFixture.RustFsEndpoint
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var userId = Guid.CreateVersion7();

        var fileType = (
            await commands.SendAsync<CreateBinaryFileTypeCommand, BinaryFileTypeResponse>(
                new CreateBinaryFileTypeCommand(
                    "Person Photos",
                    StorageProviderKey: "rustfs",
                    MaxFileSizeBytes: 5 * 1024 * 1024,
                    MaxWidth: 64,
                    MaxHeight: 64,
                    AllowedExtensions: ".png,.jpg"
                )
            )
        ).Value;

        var rawPng = CoreFilesAndSignaturesDomainTests.CreateSolidPng(128, 64);
        await using var stream = new MemoryStream(rawPng);
        var uploaded = (
            await commands.SendAsync<UploadBinaryFileCommand, BinaryFileResponse>(
                new UploadBinaryFileCommand(
                    fileType.Id,
                    "avatar photo (1).png",
                    "image/png",
                    stream,
                    IsTemporary: true,
                    UploadedByUserId: userId
                )
            )
        ).Value;

        Assert.Equal("rustfs", uploaded.StorageProviderKey);
        Assert.Equal("avatar_photo_1.png", uploaded.FileName);
        Assert.Equal(64, uploaded.Width);
        Assert.Equal(32, uploaded.Height);

        var presigned = (
            await queries.QueryAsync<GetBinaryFilePresignedUrlQuery, StoragePresignedUrl>(
                new GetBinaryFilePresignedUrlQuery(uploaded.Id, ExpiresInMinutes: 10)
            )
        ).Value;
        using var http = new HttpClient();
        var downloadedBytes = await http.GetByteArrayAsync(presigned.Url);
        Assert.Equal(uploaded.FileSize, downloadedBytes.LongLength);

        var cropped = (
            await commands.SendAsync<CropBinaryFileImageCommand, BinaryFileResponse>(
                new CropBinaryFileImageCommand(
                    uploaded.Id,
                    new ImageCropRequest(
                        X: 0,
                        Y: 0,
                        Width: 32,
                        Height: 32,
                        TargetWidth: 16,
                        TargetHeight: 16
                    ),
                    CreateCopy: false
                )
            )
        ).Value;
        Assert.Equal(16, cropped.Width);
        Assert.Equal(16, cropped.Height);
    }

    [Fact]
    public async Task Documents_Quota_Lifecycle_And_Electronic_Signatures_Work_EndToEnd()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var userId = Guid.CreateVersion7();
        var targetEntityId = Guid.CreateVersion7();

        var fileType = (
            await commands.SendAsync<CreateBinaryFileTypeCommand, BinaryFileTypeResponse>(
                new CreateBinaryFileTypeCommand("Campus Docs", StorageProviderKey: "in-memory")
            )
        ).Value;

        await using var ms = new MemoryStream(
            CoreFilesAndSignaturesDomainTests.CreateSolidPng(16, 16)
        );
        var uploaded = (
            await commands.SendAsync<UploadBinaryFileCommand, BinaryFileResponse>(
                new UploadBinaryFileCommand(
                    fileType.Id,
                    "campus.png",
                    "image/png",
                    ms,
                    IsTemporary: true,
                    UploadedByUserId: userId
                )
            )
        ).Value;

        var campusEntityType = (
            await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
                new GetEntityTypeByKeyQuery(Campus.EntityTypeKeyName)
            )
        ).Value;
        var docType = (
            await commands.SendAsync<CreateDocumentTypeCommand, DocumentTypeResponse>(
                new CreateDocumentTypeCommand(
                    "Campus Photo",
                    campusEntityType.Id,
                    fileType.Id,
                    MaxDocumentsPerEntity: 1,
                    IsImage: true
                )
            )
        ).Value;

        var doc = (
            await commands.SendAsync<CreateDocumentCommand, DocumentResponse>(
                new CreateDocumentCommand(
                    docType.Id,
                    targetEntityId,
                    uploaded.Id,
                    "Main Photo",
                    RequestingUserId: userId
                )
            )
        ).Value;

        var afterAttach = (
            await queries.QueryAsync<GetBinaryFileByIdQuery, BinaryFileResponse>(
                new GetBinaryFileByIdQuery(uploaded.Id)
            )
        ).Value;
        Assert.False(afterAttach.IsTemporary);

        var quotaFail = await commands.SendAsync<CreateDocumentCommand, DocumentResponse>(
            new CreateDocumentCommand(
                docType.Id,
                targetEntityId,
                uploaded.Id,
                "Duplicate",
                RequestingUserId: userId
            )
        );
        Assert.True(quotaFail.IsFailure);

        var deleted = await commands.SendAsync<DeleteDocumentCommand, bool>(
            new DeleteDocumentCommand(doc.Id)
        );
        Assert.True(deleted.IsSuccess);
        var afterDelete = (
            await queries.QueryAsync<GetBinaryFileByIdQuery, BinaryFileResponse>(
                new GetBinaryFileByIdQuery(uploaded.Id)
            )
        ).Value;
        Assert.True(afterDelete.IsTemporary);

        var sigTemplate = (
            await commands.SendAsync<
                CreateSignatureDocumentTemplateCommand,
                SignatureDocumentTemplateResponse
            >(
                new CreateSignatureDocumentTemplateCommand(
                    "Volunteer Agreement",
                    TemplateMarkup: "Default waiver body.",
                    SignatureType: ElectronicSignatureType.Typed
                )
            )
        ).Value;
        Assert.Equal("Default waiver body.", sigTemplate.TemplateMarkup);
        var sigDoc = (
            await commands.SendAsync<CreateSignatureDocumentCommand, SignatureDocumentResponse>(
                new CreateSignatureDocumentCommand(
                    sigTemplate.Id,
                    "2026 Agreement",
                    AppliesToEntityId: targetEntityId,
                    RenderedDocumentText: "I agree."
                )
            )
        ).Value;
        var signed = (
            await commands.SendAsync<SignSignatureDocumentCommand, SignatureDocumentResponse>(
                new SignSignatureDocumentCommand(
                    sigDoc.Id,
                    "John Smith",
                    "john@example.org",
                    userId
                )
            )
        ).Value;
        Assert.Equal(ElectronicSignatureStatus.Completed, signed.Status);
        Assert.False(string.IsNullOrWhiteSpace(signed.SignatureVerificationHash));

        var resign = await commands.SendAsync<
            SignSignatureDocumentCommand,
            SignatureDocumentResponse
        >(new SignSignatureDocumentCommand(sigDoc.Id, "Tamper"));
        Assert.True(resign.IsFailure);
    }
}
