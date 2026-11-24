using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace Ruth.E2E.Tests.Fixtures;

public static class TestOnboardingPayloads
{
    public static JsonObject CreateValidPayload(
        string organizationName = "Grace Community Church",
        string timeZoneId = "America/New_York",
        string primaryCampusName = "Main Campus",
        string? primaryCampusShortCode = "MAIN",
        string adminUserName = "admin",
        string adminEmail = "admin@gracechurch.test",
        string adminDisplayName = "System Administrator",
        string adminPassword = "Str0ng!Onboarding#2026"
    )
    {
        return new JsonObject
        {
            ["organizationName"] = organizationName,
            ["timeZoneId"] = timeZoneId,
            ["primaryCampusName"] = primaryCampusName,
            ["primaryCampusShortCode"] = primaryCampusShortCode,
            ["adminUserName"] = adminUserName,
            ["adminEmail"] = adminEmail,
            ["adminDisplayName"] = adminDisplayName,
            ["adminPassword"] = adminPassword,
        };
    }

    public static StringContent ToJsonContent(JsonObject payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
    }

    public static JsonObject CreateMissingOrganizationNamePayload()
    {
        return CreateValidPayload(organizationName: "   ");
    }

    public static JsonObject CreateInvalidTimeZonePayload()
    {
        return CreateValidPayload(timeZoneId: "Invalid/Not_A_TimeZone");
    }

    public static JsonObject CreateInvalidEmailPayload()
    {
        return CreateValidPayload(adminEmail: "not-a-valid-email");
    }

    public static JsonObject CreateWeakPasswordPayload()
    {
        return CreateValidPayload(adminPassword: "123");
    }

    public static JsonObject CreateMissingAdminUserNamePayload()
    {
        return CreateValidPayload(adminUserName: string.Empty);
    }

    public static JsonObject CreateOversizedOrganizationPayload()
    {
        return CreateValidPayload(organizationName: new string('A', 2048));
    }

    public static JsonObject CreateAdversarialEncodingPayload()
    {
        return CreateValidPayload(
            organizationName: "O'Reilly & Sons <Church> \"Hope\" 🚀",
            primaryCampusName: "North & East Campus — Building #1",
            primaryCampusShortCode: "NE-1",
            adminDisplayName: "José O'Connor <Admin>"
        );
    }

    public static void AssertIsUuidV7(Guid id)
    {
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(7, id.Version);
    }

    public static void AssertUuidV7Ordered(Guid earlier, Guid later)
    {
        AssertIsUuidV7(earlier);
        AssertIsUuidV7(later);
        Assert.True(
            earlier.CompareTo(later) < 0,
            $"Expected UUIDv7 '{earlier}' to sort strictly before '{later}'."
        );
    }

    public static void AssertUtcTimestamp(DateTimeOffset timestamp)
    {
        Assert.Equal(TimeSpan.Zero, timestamp.Offset);
        Assert.True(
            timestamp > DateTimeOffset.UnixEpoch,
            $"Expected UTC timestamp after UnixEpoch, got '{timestamp:O}'."
        );
    }

    public static void AssertUtcTimestampInRange(
        DateTimeOffset timestamp,
        DateTimeOffset minUtc,
        DateTimeOffset maxUtc
    )
    {
        AssertUtcTimestamp(timestamp);
        Assert.True(
            timestamp >= minUtc && timestamp <= maxUtc,
            $"Expected UTC timestamp '{timestamp:O}' between '{minUtc:O}' and '{maxUtc:O}'."
        );
    }
}
