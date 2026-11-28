using System.Text;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

internal static class BinaryFileNamePolicy
{
    public static Result<string> ScrubAndValidate(string rawFileName, string? allowedExtensionsCsv)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        if (FileContentSecurityInspector.HasPathTraversalOrInvalidFileName(rawFileName))
        {
            return CoreDocumentErrors.BinaryFilePathTraversalDetected;
        }

        var trimmed = rawFileName.Trim();
        var scrubbed = ScrubCharacters(trimmed);
        if (scrubbed.Length is 0 or > 255 || scrubbed.StartsWith('.') || scrubbed.EndsWith('.'))
        {
            return CoreDocumentErrors.BinaryFileNameInvalid;
        }

        if (FileContentSecurityInspector.HasBlockedExtension(scrubbed))
        {
            return CoreDocumentErrors.BinaryFileActiveContentBlocked;
        }

        var extension = Path.GetExtension(scrubbed);
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
