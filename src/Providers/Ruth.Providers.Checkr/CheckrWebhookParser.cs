using System.Text.Json;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.SharedKernel.Results;

namespace Ruth.Providers.Checkr;

/// <summary>Parses Checkr webhook JSON payloads into normalized <see cref="BackgroundCheckWebhookEvent" /> records.</summary>
public static class CheckrWebhookParser
{
    private static readonly DomainError _invalidPayload = DomainError.Validation(
        "Checkr.Webhook.InvalidPayload",
        "Checkr webhook payload is missing required event type or candidate identifier."
    );

    /// <summary>Parses raw Checkr webhook JSON into a normalized event.</summary>
    public static Result<BackgroundCheckWebhookEvent> Parse(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return _invalidPayload;
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;

            var eventId = TryGetString(root, "id") ?? Guid.CreateVersion7().ToString("D");
            var eventType = TryGetString(root, "type");
            if (
                string.IsNullOrWhiteSpace(eventType)
                || !root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var obj)
            )
            {
                return _invalidPayload;
            }

            var candidateId = TryGetString(obj, "candidate_id");
            if (string.IsNullOrWhiteSpace(candidateId))
            {
                return _invalidPayload;
            }

            var objectId = TryGetString(obj, "id");
            var objectStatus = TryGetString(obj, "status");
            var package = TryGetString(obj, "package");
            var (mappedStatus, recordFound, recommendation) = MapEvent(eventType, objectStatus);

            return Result.Success(
                new BackgroundCheckWebhookEvent(
                    eventId,
                    eventType,
                    candidateId,
                    objectId,
                    package,
                    mappedStatus,
                    recordFound,
                    recommendation,
                    rawPayload
                )
            );
        }
        catch (JsonException)
        {
            return _invalidPayload;
        }
    }

    private static (
        BackgroundCheckStatus Status,
        bool? RecordFound,
        string Recommendation
    ) MapEvent(string eventType, string? objectStatus)
    {
        return string.Equals(eventType, "report.completed", StringComparison.OrdinalIgnoreCase)
            ? MapReportOutcome(objectStatus)
            : eventType.ToLowerInvariant() switch
            {
                "invitation.created" => (
                    BackgroundCheckStatus.InvitationSent,
                    null,
                    "Invitation Sent"
                ),
                "invitation.completed" => (
                    BackgroundCheckStatus.InvitationCompleted,
                    null,
                    "Invitation Completed"
                ),
                "invitation.expired" => (
                    BackgroundCheckStatus.InvitationExpired,
                    null,
                    "Invitation Expired"
                ),
                "report.created" or "report.upgraded" or "report.resumed" => (
                    BackgroundCheckStatus.ReportPending,
                    null,
                    "Report Pending"
                ),
                "report.suspended" => (
                    BackgroundCheckStatus.ReportSuspended,
                    null,
                    "Report Suspended"
                ),
                "report.disputed" => (
                    BackgroundCheckStatus.ReportSuspended,
                    null,
                    "Report Disputed"
                ),
                "report.updated" => MapReportOutcome(objectStatus),
                _ => (BackgroundCheckStatus.ReportPending, null, eventType),
            };
    }

    private static (
        BackgroundCheckStatus Status,
        bool? RecordFound,
        string Recommendation
    ) MapReportOutcome(string? objectStatus)
    {
        return (objectStatus ?? string.Empty).ToLowerInvariant() switch
        {
            "clear" => (BackgroundCheckStatus.ReportClear, false, "Candidate Pass"),
            "consider" => (BackgroundCheckStatus.ReportConsider, true, "Candidate Review"),
            "suspended" => (BackgroundCheckStatus.ReportSuspended, null, "Report Suspended"),
            "dispute" => (BackgroundCheckStatus.ReportSuspended, null, "Report Disputed"),
            _ => (BackgroundCheckStatus.ReportPending, null, "Report Pending"),
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return
            element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}
