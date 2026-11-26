using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Domain;

public sealed class W3S5SystemCommunicationAndPhoneDomainTests
{
    [Fact]
    public void SystemCommunication_ValidatesSlugEmailAndBuiltInDeleteProtection()
    {
        var invalidKey = SystemCommunication.Create(
            new SystemCommunicationSpec(
                "Bad Key",
                "123-invalid",
                SystemCommunicationCategory.General,
                "Subject",
                "<p>Body</p>"
            )
        );
        Assert.True(invalidKey.IsFailure);
        Assert.Equal(
            CommunicationDomainErrors.SystemCommunicationKeyInvalid.Code,
            invalidKey.PrimaryError.Code
        );

        var invalidEmail = SystemCommunication.Create(
            new SystemCommunicationSpec(
                "Bad Sender",
                "general.bad-sender",
                SystemCommunicationCategory.General,
                "Subject",
                "<p>Body</p>",
                FromEmail: "not-an-email"
            )
        );
        Assert.True(invalidEmail.IsFailure);
        Assert.Equal(
            CommunicationDomainErrors.SystemCommunicationEmailInvalid.Code,
            invalidEmail.PrimaryError.Code
        );

        var builtIn = SystemCommunication.Create(
            new SystemCommunicationSpec(
                "Password Reset",
                "Identity.Password-Reset",
                SystemCommunicationCategory.IdentityAndSecurity,
                "Reset {{ UserName }}",
                "<p>Code: {{ ResetToken }}</p>",
                FromEmail: "security@ruth.local",
                IsSystem: true
            )
        );
        Assert.True(builtIn.IsSuccess);
        Assert.Equal("identity.password-reset", builtIn.Value.Key);
        Assert.True(builtIn.Value.EnsureCanDelete().IsFailure);

        var custom = SystemCommunication.Create(
            new SystemCommunicationSpec(
                "Custom Alert",
                "general.custom-alert",
                SystemCommunicationCategory.General,
                "Hello",
                "<p>World</p>",
                IsSystem: false
            )
        );
        Assert.True(custom.IsSuccess);
        Assert.True(custom.Value.EnsureCanDelete().IsSuccess);
    }

    [Theory]
    [InlineData("(555) 234-5678", "+15552345678")]
    [InlineData("+44 20 7946 0958", "+442079460958")]
    [InlineData("15559876543", "+15559876543")]
    public void SystemPhoneNumber_NormalizesValidNumbersToE164_AndRepointsPerson(
        string raw,
        string expectedE164
    )
    {
        var retiredId = Guid.CreateVersion7();
        var survivingId = Guid.CreateVersion7();

        var created = SystemPhoneNumber.Create("Support Desk", raw, assignedToPersonId: retiredId);
        Assert.True(created.IsSuccess);
        Assert.Equal(expectedE164, created.Value.Number);

        created.Value.RepointAssignedPerson(retiredId, survivingId);
        Assert.Equal(survivingId, created.Value.AssignedToPersonId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("+0123456789")]
    [InlineData("12345")]
    public void SystemPhoneNumber_RejectsMalformedE164Numbers(string invalid)
    {
        var res = SystemPhoneNumber.Create("Invalid", invalid);
        Assert.True(res.IsFailure);
        Assert.Equal(
            CommunicationDomainErrors.SystemPhoneNumberInvalid.Code,
            res.PrimaryError.Code
        );
    }

    [Fact]
    public void PersonalDevice_ValidatesPushTokens_RefreshesHeartbeat_AndRepointsPerson()
    {
        var now = DateTimeOffset.UtcNow;
        var bad = PersonalDevice.Register(
            PersonalDevicePlatform.Ios,
            "short",
            personId: null,
            deviceName: null,
            deviceVersion: null,
            notificationsEnabled: true,
            now
        );
        Assert.True(bad.IsFailure);

        var retired = Guid.CreateVersion7();
        var surviving = Guid.CreateVersion7();
        var device = PersonalDevice
            .Register(
                PersonalDevicePlatform.Android,
                "fcm-token-abcdef0123456789",
                retired,
                "Pixel 9",
                "15.0",
                notificationsEnabled: true,
                now
            )
            .Value;

        device.SetNotificationPreferences(notificationsEnabled: false, isActive: false);
        Assert.False(device.NotificationsEnabled);
        Assert.False(device.IsActive);

        device.RefreshRegistration(
            PersonalDevicePlatform.Android,
            retired,
            "Pixel 9 Pro",
            "15.1",
            notificationsEnabled: true,
            now.AddMinutes(5)
        );
        Assert.True(device.IsActive);
        Assert.Equal("Pixel 9 Pro", device.DeviceName);

        device.RepointPerson(retired, surviving);
        Assert.Equal(surviving, device.PersonId);
    }
}
