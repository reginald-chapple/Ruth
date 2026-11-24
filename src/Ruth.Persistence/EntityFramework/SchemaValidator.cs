using System.Text.RegularExpressions;

namespace Ruth.Persistence.EntityFramework;

internal static partial class SchemaValidator
{
    [GeneratedRegex(
        "^[a-z_][a-z0-9_]{0,62}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex ValidSchemaRegex { get; }

    public static string Validate(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        return ValidSchemaRegex.IsMatch(schema)
            ? schema
            : throw new ArgumentException(
                $"Database schema '{schema}' must match PostgreSQL identifier pattern ^[a-z_][a-z0-9_]{{0,62}}$.",
                nameof(schema)
            );
    }
}
