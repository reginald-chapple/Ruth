using System.Collections.Frozen;

namespace Ruth.SharedKernel.Security;

/// <summary>Magic-byte signature, polyglot marker, and extension-to-MIME lookup tables for <see cref="FileContentSecurityInspector" />.</summary>
internal static class FileMagicSignatureCatalog
{
    private const int MaxPolyglotScanWindowBytes = 65_536;

    public static readonly FrozenSet<string> AllowedCheckImageMimes = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "image/png",
        "image/jpeg",
        "image/tiff",
        "application/pdf"
    );

    public static readonly FrozenSet<string> StrictSignatureMimes = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/tiff",
        "image/bmp",
        "application/pdf",
        "application/zip"
    );

    public static readonly FrozenSet<string> PlainTextExtensions = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ".txt",
        ".csv",
        ".ics",
        ".json"
    );

    private static readonly string[] _activeContentMarkers =
    [
        "<svg",
        "<script",
        "<!doctype html",
        "<html",
        "<iframe",
        "<object",
        "<embed",
        "<!entity",
        "<?php",
        "<%@",
        "javascript:",
        "vbscript:",
        "onerror=",
        "onload=",
        "<foreignobject",
    ];

    private static readonly FrozenDictionary<string, string> _expectedMagicByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".bmp"] = "image/bmp",
            [".pdf"] = "application/pdf",
            [".zip"] = "application/zip",
            [".docx"] = "application/zip",
            [".xlsx"] = "application/zip",
            [".pptx"] = "application/zip",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> _ooxmlMimeByExtension = new Dictionary<
        string,
        string
    >(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static string? SniffKnownMagicMime(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 8
            && bytes[..8]
                .SequenceEqual((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
                ? "image/png"
            : bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF
                ? "image/jpeg"
            : bytes.Length >= 6
            && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8))
                ? "image/gif"
            : bytes.Length >= 12
            && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
                ? "image/webp"
            : SniffDocumentOrRasterMime(bytes);
    }

    public static bool HasExecutableOrActivePolyglot(ReadOnlySpan<byte> bytes)
    {
        return HasExecutableSignature(bytes) || ContainsActiveOrPolyglotMarkers(bytes);
    }

    public static UploadValidationResult VerifyMagicBytesAgainstDeclaration(
        ReadOnlySpan<byte> bytes,
        string fileName,
        string? declaredMimeType
    )
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var declared = ExtractBaseMediaType(declaredMimeType);
        var sniffed = SniffKnownMagicMime(bytes);
        var expectedMimeForExt = _expectedMagicByExtension.GetValueOrDefault(ext);

        return expectedMimeForExt is not null
                ? ValidateExpectedExtensionSignature(sniffed, declared, expectedMimeForExt, ext)
            : !string.IsNullOrEmpty(declared)
            && StrictSignatureMimes.Contains(declared)
            && !string.Equals(sniffed, declared, StringComparison.OrdinalIgnoreCase)
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.MimeMagicMismatch",
                    $"Payload magic bytes do not match declared MIME '{declared}'."
                )
            : PlainTextExtensions.Contains(ext) && bytes.Contains((byte)0)
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.MimeMagicMismatch",
                    "Text/CSV/JSON upload contains binary null bytes."
                )
            : UploadValidationResult.Success(
                sniffed ?? (string.IsNullOrEmpty(declared) ? "application/octet-stream" : declared)
            );
    }

    public static string ExtractBaseMediaType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return string.Empty;
        }

        var trimmed = mimeType.Trim();
        var semi = trimmed.IndexOf(';', StringComparison.Ordinal);
        return (semi >= 0 ? trimmed[..semi] : trimmed).Trim().ToLowerInvariant();
    }

    private static UploadValidationResult ValidateExpectedExtensionSignature(
        string? sniffed,
        string declared,
        string expectedMimeForExt,
        string ext
    )
    {
        var canonicalMime = _ooxmlMimeByExtension.GetValueOrDefault(ext) ?? expectedMimeForExt;
        return
            !string.Equals(sniffed, expectedMimeForExt, StringComparison.OrdinalIgnoreCase)
            || (
                !string.IsNullOrEmpty(declared)
                && !string.Equals(
                    declared,
                    "application/octet-stream",
                    StringComparison.OrdinalIgnoreCase
                )
                && !string.Equals(declared, expectedMimeForExt, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(declared, canonicalMime, StringComparison.OrdinalIgnoreCase)
            )
            ? UploadValidationResult.Fail(
                "Core.BinaryFile.MimeMagicMismatch",
                $"Payload magic bytes do not match extension '{ext}' and declared MIME '{declared}'."
            )
            : UploadValidationResult.Success(canonicalMime);
    }

    private static bool HasExecutableSignature(ReadOnlySpan<byte> bytes)
    {
        return (bytes.Length >= 2 && bytes[0] == 0x4D && bytes[1] == 0x5A)
            || (bytes.Length >= 2 && bytes[0] == 0x23 && bytes[1] == 0x21)
            || (
                bytes.Length >= 4
                && bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0x7F, 0x45, 0x4C, 0x46])
            )
            || (
                bytes.Length >= 4
                && bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0xCA, 0xFE, 0xBA, 0xBE])
            )
            || (
                bytes.Length >= 4
                && bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0xCF, 0xFA, 0xED, 0xFE])
            )
            || (
                bytes.Length >= 4
                && bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0xFE, 0xED, 0xFA, 0xCF])
            );
    }

    private static bool ContainsActiveOrPolyglotMarkers(ReadOnlySpan<byte> bytes)
    {
        var window = bytes[..Math.Min(bytes.Length, MaxPolyglotScanWindowBytes)];
        var chars = new char[window.Length];
        for (var i = 0; i < window.Length; i++)
        {
            var b = window[i];
            chars[i] = b is >= 0x20 and <= 0x7E ? (char)b : ' ';
        }

        var asciiView = new string(chars);
        foreach (var marker in _activeContentMarkers)
        {
            if (asciiView.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? SniffDocumentOrRasterMime(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8) ? "application/pdf"
            : bytes.Length >= 4
            && (
                bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0x49, 0x49, 0x2A, 0x00])
                || bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0x4D, 0x4D, 0x00, 0x2A])
            )
                ? "image/tiff"
            : bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D ? "image/bmp"
            : bytes.Length >= 4
            && bytes[..4].SequenceEqual((ReadOnlySpan<byte>)[0x50, 0x4B, 0x03, 0x04])
                ? "application/zip"
            : null;
    }
}
