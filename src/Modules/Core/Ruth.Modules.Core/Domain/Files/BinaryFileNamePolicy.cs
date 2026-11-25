using System.Collections.Frozen;
using System.Text;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Files;

internal static class BinaryFileNamePolicy
{
    private static readonly FrozenSet<string> _blockedExtensions = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ".exe",
        ".dll",
        ".bat",
        ".cmd",
        ".sh",
        ".ps1",
        ".msi",
        ".vbs",
        ".js",
        ".com",
        ".scr",
        ".pif",
        ".aspx",
        ".ascx",
        ".cshtml",
        ".php"
    );

    public static Result<string> ScrubAndValidate(string rawFileName, string? allowedExtensionsCsv)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        var leaf = Path.GetFileName(rawFileName.Trim());
        if (string.IsNullOrWhiteSpace(leaf) || leaf.EndsWith('.'))
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        var scrubbed = ScrubCharacters(leaf);
        if (scrubbed.Length is 0 or > 255)
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        var extension = Path.GetExtension(scrubbed);
        if (!string.IsNullOrEmpty(extension) && _blockedExtensions.Contains(extension))
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        if (!string.IsNullOrWhiteSpace(allowedExtensionsCsv))
        {
            var allowed = ParseAllowedExtensions(allowedExtensionsCsv);
            if (allowed.Count > 0 && !allowed.Contains(extension))
            {
                return CoreDocumentErrors.BinaryFileExtensionNotAllowed;
            }
        }

        return Result.Success(scrubbed);
    }

    private static string ScrubCharacters(string fileName)
    {
        var sb = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            if (
                char.IsControl(ch)
                || ch
                    is '<'
                        or '>'
                        or ':'
                        or '"'
                        or '/'
                        or '\\'
                        or '|'
                        or '?'
                        or '*'
                        or '#'
                        or '('
                        or ')'
                        or '&'
                        or '%'
            )
            {
                continue;
            }

            _ = sb.Append(ch == ' ' ? '_' : ch);
        }

        return sb.ToString();
    }

    private static HashSet<string> ParseAllowedExtensions(string csv)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (
            var raw in csv.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
        {
            var normalized = raw.StartsWith('.') ? raw : $".{raw}";
            _ = set.Add(normalized);
        }

        return set;
    }
}
