using System.Collections.Frozen;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Deterministic magic-byte MIME verifier, polyglot/SVG/HTML active-content blocker,
/// and path-traversal guard for uploaded files and encrypted check images.
/// </summary>
public static class FileContentSecurityInspector
{
    private static readonly FrozenSet<string> _blockedExtensions = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ".svg",
        ".svgz",
        ".html",
        ".htm",
        ".xhtml",
        ".shtml",
        ".mhtml",
        ".hta",
        ".xml",
        ".xsl",
        ".xslt",
        ".js",
        ".mjs",
        ".cjs",
        ".ts",
        ".exe",
        ".dll",
        ".bat",
        ".cmd",
        ".sh",
        ".ps1",
        ".msi",
        ".vbs",
        ".com",
        ".scr",
        ".pif",
        ".aspx",
        ".ascx",
        ".cshtml",
        ".razor",
        ".php",
        ".jsp",
        ".jspx",
        ".jar",
        ".war",
        ".py",
        ".rb",
        ".pl",
        ".cgi",
        ".swf",
        ".wasm",
        ".lnk",
        ".scf",
        ".url"
    );

    private static readonly FrozenSet<string> _blockedMimeTypes = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "image/svg+xml",
        "text/html",
        "application/xhtml+xml",
        "text/javascript",
        "application/javascript",
        "application/x-javascript",
        "application/ecmascript",
        "text/xml",
        "application/xml",
        "application/x-msdownload",
        "application/x-executable",
        "application/x-dosexec",
        "application/x-sh",
        "application/x-httpd-php",
        "multipart/related"
    );

    private static readonly string[] _encodedTraversalTokens =
    [
        "%2e",
        "%2f",
        "%5c",
        "%00",
        "%252e",
        "%252f",
        "%255c",
        "%c0%af",
        "%c1%9c",
    ];

    /// <summary>Returns true when <paramref name="rawPathOrName" /> contains traversal sequences, separators, or control characters.</summary>
    public static bool HasPathTraversalOrInvalidFileName(string? rawPathOrName)
    {
        if (string.IsNullOrWhiteSpace(rawPathOrName))
        {
            return true;
        }

        var trimmed = rawPathOrName.Trim();
        return trimmed.StartsWith('.')
            || trimmed.EndsWith('.')
            || trimmed.Contains("..", StringComparison.Ordinal)
            || ContainsSeparatorOrControl(trimmed)
            || ContainsEncodedTraversal(trimmed);
    }

    /// <summary>Returns true when <paramref name="rawKey" /> contains relative/encoded path traversal or control characters.</summary>
    public static bool HasStorageKeyTraversal(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || ContainsEncodedTraversal(rawKey))
        {
            return true;
        }

        foreach (var ch in rawKey)
        {
            if (char.IsControl(ch) || ch == '\\')
            {
                return true;
            }
        }

        foreach (var segment in rawKey.Trim().Split('/'))
        {
            if (
                string.Equals(segment, ".", StringComparison.Ordinal)
                || string.Equals(segment, "..", StringComparison.Ordinal)
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true if any extension segment on <paramref name="fileName" /> is blocked active/executable content.</summary>
    public static bool HasBlockedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        var parts = fileName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < parts.Length; i++)
        {
            if (_blockedExtensions.Contains($".{parts[i]}"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when <paramref name="mimeType" /> is an active/executable content type blocked from user upload.</summary>
    public static bool IsBlockedActiveMimeType(string? mimeType)
    {
        return !string.IsNullOrWhiteSpace(mimeType)
            && _blockedMimeTypes.Contains(FileMagicSignatureCatalog.ExtractBaseMediaType(mimeType));
    }

    /// <summary>Validates file name, declared MIME type, magic-byte header, and polyglot/SVG/HTML absence.</summary>
    public static UploadValidationResult InspectAndValidateUpload(
        ReadOnlySpan<byte> bytes,
        string fileName,
        string? declaredMimeType
    )
    {
        return HasPathTraversalOrInvalidFileName(fileName)
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.PathTraversalDetected",
                    "File name contains prohibited path traversal or control characters."
                )
            : HasBlockedExtension(fileName) || IsBlockedActiveMimeType(declaredMimeType)
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.ActiveContentBlocked",
                    "SVG, HTML, script, and executable active content uploads are prohibited."
                )
            : bytes.IsEmpty
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.EmptyPayload",
                    "Uploaded binary payload cannot be empty."
                )
            : FileMagicSignatureCatalog.HasExecutableOrActivePolyglot(bytes)
                ? UploadValidationResult.Fail(
                    "Core.BinaryFile.ActiveContentBlocked",
                    "Binary payload contains active HTML/SVG/script or executable polyglot content."
                )
            : FileMagicSignatureCatalog.VerifyMagicBytesAgainstDeclaration(
                bytes,
                fileName,
                declaredMimeType
            );
    }

    /// <summary>Validates check/remittance image bytes against PNG/JPEG/TIFF/PDF magic signatures and blocks active/polyglot payloads.</summary>
    public static UploadValidationResult ValidateCheckImage(
        ReadOnlySpan<byte> bytes,
        string? declaredContentType
    )
    {
        if (bytes.IsEmpty || HasHeaderInjectionChars(declaredContentType))
        {
            return UploadValidationResult.Fail(
                "Finance.CheckImageInvalid",
                "Check image payload is empty or content type is invalid."
            );
        }

        if (
            IsBlockedActiveMimeType(declaredContentType)
            || FileMagicSignatureCatalog.HasExecutableOrActivePolyglot(bytes)
        )
        {
            return UploadValidationResult.Fail(
                "Finance.CheckImageActiveContentBlocked",
                "Check image contains prohibited SVG/HTML/executable or polyglot content."
            );
        }

        var sniffed = FileMagicSignatureCatalog.SniffKnownMagicMime(bytes);
        var normalizedDeclared = string.IsNullOrWhiteSpace(declaredContentType)
            ? "image/png"
            : FileMagicSignatureCatalog.ExtractBaseMediaType(declaredContentType);

        return
            sniffed is null
            || !FileMagicSignatureCatalog.AllowedCheckImageMimes.Contains(sniffed)
            || !string.Equals(sniffed, normalizedDeclared, StringComparison.OrdinalIgnoreCase)
            ? UploadValidationResult.Fail(
                "Finance.CheckImageMimeMismatch",
                "Check image magic bytes do not match an allowed image/PDF format."
            )
            : UploadValidationResult.Success(sniffed);
    }

    private static bool ContainsEncodedTraversal(string text)
    {
        foreach (var token in _encodedTraversalTokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsSeparatorOrControl(string text)
    {
        foreach (var ch in text)
        {
            if (char.IsControl(ch) || ch is '/' or '\\' or ':')
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasHeaderInjectionChars(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }
}
