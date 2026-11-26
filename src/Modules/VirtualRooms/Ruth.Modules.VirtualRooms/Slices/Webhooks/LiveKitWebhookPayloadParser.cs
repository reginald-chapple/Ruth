using System.Text.Json;

namespace Ruth.Modules.VirtualRooms.Slices.Webhooks;

internal static class LiveKitWebhookPayloadParser
{
    public static ParsedLiveKitWebhook? TryParse(string rawJson, DateTimeOffset fallbackNowUtc)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            var evt = GetString(root, "event");
            if (string.IsNullOrWhiteSpace(evt))
            {
                return null;
            }

            var timestamp = ParseTimestamp(root, fallbackNowUtc);
            var (roomName, roomSid) = ParseRoom(root);
            var (identity, personId, name) = ParseParticipant(root);
            var (egressId, egRoom, filePath, sizeBytes, durationSec, egError) = ParseEgress(root);

            return new ParsedLiveKitWebhook(
                evt.Trim(),
                timestamp,
                roomName ?? egRoom,
                roomSid,
                identity,
                personId,
                name,
                egressId,
                filePath,
                sizeBytes,
                durationSec,
                egError
            );
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateTimeOffset ParseTimestamp(JsonElement root, DateTimeOffset fallback)
    {
        return root.TryGetProperty("createdAt", out var p1)
            && p1.TryGetInt64(out var sec1)
            && sec1 > 0
                ? DateTimeOffset.FromUnixTimeSeconds(sec1)
            : root.TryGetProperty("created_at", out var p2)
            && p2.TryGetInt64(out var sec2)
            && sec2 > 0
                ? DateTimeOffset.FromUnixTimeSeconds(sec2)
            : fallback;
    }

    private static (string? Name, string? Sid) ParseRoom(JsonElement root)
    {
        if (!root.TryGetProperty("room", out var room) || room.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        return (GetString(room, "name"), GetString(room, "sid"));
    }

    private static (string? Identity, Guid? PersonId, string? Name) ParseParticipant(
        JsonElement root
    )
    {
        if (
            !root.TryGetProperty("participant", out var part)
            || part.ValueKind != JsonValueKind.Object
        )
        {
            return (null, null, null);
        }

        var identity = GetString(part, "identity");
        var name = GetString(part, "name");
        Guid? personId = null;
        if (
            identity is not null
            && identity.StartsWith("person:", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(identity[7..], out var parsed)
        )
        {
            personId = parsed;
        }

        return (identity, personId, name);
    }

    private static (
        string? EgressId,
        string? RoomName,
        string? FilePath,
        long SizeBytes,
        int DurationSeconds,
        string? Error
    ) ParseEgress(JsonElement root)
    {
        var propName =
            root.TryGetProperty("egressInfo", out var eg) ? "egressInfo"
            : root.TryGetProperty("egress_info", out eg) ? "egress_info"
            : null;
        if (propName is null || eg.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null, 0L, 0, null);
        }

        var id = GetString(eg, "egressId") ?? GetString(eg, "egress_id");
        var roomName = GetString(eg, "roomName") ?? GetString(eg, "room_name");
        var error = GetString(eg, "error");
        var (path, size, dur) = ParseEgressFileResult(eg);
        return (id, roomName, path, size, dur, error);
    }

    private static (string? Path, long Size, int DurationSec) ParseEgressFileResult(JsonElement eg)
    {
        return TryGetFirstFileElement(eg, out var first)
            ? ExtractFileMetrics(first)
            : (GetString(eg, "filepath"), 0L, 0);
    }

    private static bool TryGetFirstFileElement(JsonElement eg, out JsonElement first)
    {
        if (
            (
                eg.TryGetProperty("fileResults", out var arr)
                || eg.TryGetProperty("file_results", out arr)
            )
            && arr.ValueKind == JsonValueKind.Array
            && arr.GetArrayLength() > 0
        )
        {
            first = arr[0];
            return true;
        }

        first = default;
        return false;
    }

    private static (string? Path, long Size, int DurationSec) ExtractFileMetrics(JsonElement first)
    {
        var filename = GetString(first, "filename") ?? GetString(first, "location");
        var size = first.TryGetProperty("size", out var sz) && sz.TryGetInt64(out var s) ? s : 0L;
        var durNs =
            first.TryGetProperty("duration", out var d) && d.TryGetInt64(out var rawD) ? rawD : 0L;
        var durSec = durNs > 1_000_000_000L ? (int)(durNs / 1_000_000_000L) : (int)durNs;
        return (filename, size, durSec);
    }

    private static string? GetString(JsonElement obj, string propName)
    {
        return obj.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }
}
