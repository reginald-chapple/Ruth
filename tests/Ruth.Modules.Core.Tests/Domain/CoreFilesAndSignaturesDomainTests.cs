using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure.Imaging;
using StbImageWriteSharp;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreFilesAndSignaturesDomainTests
{
    [Fact]
    public void BinaryFileNamePolicy_Scrubs_And_Blocks_Dangerous_Extensions()
    {
        var scrubbed = BinaryFileNamePolicy.ScrubAndValidate("my photo (1) #test.png", ".png,.jpg");
        Assert.True(scrubbed.IsSuccess);
        Assert.Equal("my_photo_1_test.png", scrubbed.Value);

        Assert.True(
            BinaryFileNamePolicy
                .ScrubAndValidate("malware.exe", allowedExtensionsCsv: null)
                .IsFailure
        );
        Assert.True(
            BinaryFileNamePolicy
                .ScrubAndValidate("script.ps1", allowedExtensionsCsv: null)
                .IsFailure
        );
        Assert.True(BinaryFileNamePolicy.ScrubAndValidate("doc.pdf", ".png,.jpg").IsFailure);
        Assert.True(
            BinaryFileNamePolicy
                .ScrubAndValidate("trailingdot.", allowedExtensionsCsv: null)
                .IsFailure
        );
    }

    [Fact]
    public void StbImageProcessor_Reads_Dimensions_Resizes_And_Crops_Png_And_Jpeg()
    {
        var pngBytes = CreateSolidPng(width: 64, height: 32);
        var processor = new StbImageProcessor();

        var dims = processor.TryReadDimensions(pngBytes);
        Assert.True(dims.IsSuccess);
        Assert.Equal((64, 32), dims.Value);

        var resized = processor.ResizeToBoundsIfNeeded(
            pngBytes,
            "image/png",
            maxWidth: 32,
            maxHeight: 32
        );
        Assert.True(resized.IsSuccess);
        Assert.Equal(32, resized.Value.Width);
        Assert.Equal(16, resized.Value.Height);

        var cropped = processor.CropAndResize(
            pngBytes,
            new ImageCropRequest(
                X: 8,
                Y: 4,
                Width: 24,
                Height: 24,
                TargetWidth: 16,
                TargetHeight: 16,
                OutputFormat: "jpg"
            )
        );
        Assert.True(cropped.IsSuccess);
        Assert.Equal("image/jpeg", cropped.Value.MimeType);
        Assert.Equal(16, cropped.Value.Width);
        Assert.Equal(16, cropped.Value.Height);

        var badBounds = processor.CropAndResize(
            pngBytes,
            new ImageCropRequest(X: 50, Y: 0, Width: 30, Height: 10)
        );
        Assert.True(badBounds.IsFailure);
        Assert.True(
            processor.CropAndResize(pngBytes, new ImageCropRequest(-1, 0, 10, 10)).IsFailure
        );
        Assert.True(
            processor.CropAndResize(pngBytes, new ImageCropRequest(0, -1, 10, 10)).IsFailure
        );
        Assert.True(processor.CropAndResize(pngBytes, new ImageCropRequest(0, 0, 0, 10)).IsFailure);
        Assert.True(processor.CropAndResize(pngBytes, new ImageCropRequest(0, 0, 10, 0)).IsFailure);
        Assert.True(
            processor.CropAndResize(pngBytes, new ImageCropRequest(0, 30, 10, 10)).IsFailure
        );
        Assert.True(processor.CropAndResize([1, 2, 3], new ImageCropRequest(0, 0, 1, 1)).IsFailure);

        var cropOnlyPng = processor.CropAndResize(
            pngBytes,
            new ImageCropRequest(
                X: 0,
                Y: 0,
                Width: 16,
                Height: 16,
                TargetWidth: null,
                TargetHeight: null,
                OutputFormat: "png"
            )
        );
        Assert.True(cropOnlyPng.IsSuccess);
        Assert.Equal("image/png", cropOnlyPng.Value.MimeType);
        Assert.Equal(16, cropOnlyPng.Value.Width);
    }

    [Fact]
    public void SignatureDocument_Enforces_Drawn_Png_Regex_Seals_Hash_And_Blocks_Resigning()
    {
        var template = SignatureDocumentTemplate
            .Create(
                "Youth Camp Waiver",
                signatureType: ElectronicSignatureType.Drawn,
                isValidInFuture: true,
                validityDurationInDays: 365
            )
            .Value;

        var doc = SignatureDocument
            .Create(
                template.Id,
                "2026 Youth Camp Waiver",
                renderedDocumentText: "I agree to camp terms."
            )
            .Value;

        // SSRF / HTML injection payload must be rejected
        var invalidDraw = doc.Sign(
            template.SignatureType,
            "Jane Doe",
            "jane@example.org",
            Guid.CreateVersion7(),
            drawnSignatureDataUrl: "https://evil.example/ssrf.png",
            signedDocumentText: null,
            clientIp: "192.0.2.10",
            clientUserAgent: "TestAgent",
            signedOnUtc: DateTimeOffset.UtcNow
        );
        Assert.True(invalidDraw.IsFailure);
        Assert.Equal(
            CoreDocumentErrors.SignatureDrawnDataInvalid.Code,
            invalidDraw.PrimaryError.Code
        );

        var validPngBase64 =
            $"data:image/png;base64,{Convert.ToBase64String(CreateSolidPng(4, 4))}";
        var signedAt = new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.Zero);
        var signed = doc.Sign(
            template.SignatureType,
            "Jane Doe",
            "jane@example.org",
            Guid.CreateVersion7(),
            validPngBase64,
            signedDocumentText: "I agree to camp terms.",
            clientIp: "192.0.2.10",
            clientUserAgent: "TestAgent",
            signedOnUtc: signedAt
        );
        Assert.True(signed.IsSuccess);
        Assert.Equal(ElectronicSignatureStatus.Completed, doc.Status);
        Assert.True(doc.IsSealed);
        Assert.True(doc.VerifyIntegrity());
        Assert.True(template.IsSignatureValidAsOf(signedAt, signedAt.AddDays(100)));
        Assert.False(template.IsSignatureValidAsOf(signedAt, signedAt.AddDays(400)));

        // Re-signing an already-sealed document must fail
        var resign = doc.Sign(
            template.SignatureType,
            "Tampered Name",
            signedByEmail: null,
            signedByUserId: null,
            validPngBase64,
            signedDocumentText: null,
            clientIp: null,
            clientUserAgent: null,
            signedAt.AddMinutes(5)
        );
        Assert.True(resign.IsFailure);
        Assert.Equal(
            CoreDocumentErrors.SignatureDocumentAlreadySigned.Code,
            resign.PrimaryError.Code
        );
    }

    internal static byte[] CreateSolidPng(int width, int height)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = 37;
            rgba[i + 1] = 99;
            rgba[i + 2] = 235;
            rgba[i + 3] = 255;
        }

        using var ms = new MemoryStream();
        new ImageWriter().WritePng(rgba, width, height, ColorComponents.RedGreenBlueAlpha, ms);
        return ms.ToArray();
    }
}
