using System.Globalization;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Ruth.Modules.Crm.Infrastructure.Graph;

internal static class ApacheAgeCypherHelper
{
    public const string GraphName = "crm_graph";

    public static string BuildCypherSql(string cypher, string returnColumns, string selectList)
    {
        return $"SELECT {selectList} FROM ag_catalog.cypher('{GraphName}', $cypher${cypher}$cypher$) AS ({returnColumns});";
    }

    public static string FormatGuid(Guid value)
    {
        return value.ToString("D", CultureInfo.InvariantCulture);
    }

    public static string EscapeCypher(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input.Trim())
        {
            if (char.IsControl(ch) || ch == '$')
            {
                continue;
            }

            if (ch is '\'' or '\\')
            {
                _ = sb.Append('\\');
            }

            _ = sb.Append(ch);
        }

        return sb.ToString();
    }

    public static string? ReadAgString(NpgsqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var text = reader.GetString(ordinal).Trim();
        if (
            string.IsNullOrWhiteSpace(text) || string.Equals(text, "null", StringComparison.Ordinal)
        )
        {
            return null;
        }

        if (text.StartsWith('"') && text.EndsWith('"'))
        {
            try
            {
                return JsonSerializer.Deserialize<string>(text);
            }
            catch (JsonException)
            {
                return text.Trim('"');
            }
        }

        return text;
    }

    public static int ReadAgInt(NpgsqlDataReader reader, int ordinal)
    {
        return int.TryParse(
            ReadAgString(reader, ordinal),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var val
        )
            ? val
            : 0;
    }
}
