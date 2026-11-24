using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Domain;

public sealed class IdentityDomainTests
{
    [Fact]
    public void StronglyTypedIds_CreateUuidV7AndRoundTrip()
    {
        var userId = UserAccountId.Create();
        var roleId = SecurityRoleId.Create();
        var orgId = OrganizationId.Create();

        Assert.NotEqual(UserAccountId.Empty, userId);
        Assert.NotEqual(SecurityRoleId.Empty, roleId);
        Assert.NotEqual(OrganizationId.Empty, orgId);
        Assert.Equal(7, userId.Value.Version);
        Assert.Equal(7, roleId.Value.Version);
        Assert.Equal(7, orgId.Value.Version);
        Assert.Equal(userId, UserAccountId.From(userId.Value));
        Assert.Equal(roleId, SecurityRoleId.From(roleId.Value));
        Assert.Equal(orgId, OrganizationId.From(orgId.Value));
    }

    [Fact]
    public void UserAccount_CreateAdministrator_NormalizesAssignsRoleAndNeverLeaksHashInToString()
    {
        var createdAt = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.FromHours(-5));

        var result = UserAccount.CreateAdministrator(
            "  admin.ruth  ",
            "  Admin@GraceChurch.org ",
            "  Jane Doe ",
            "PBKDF2_HASH_SECRET_VALUE",
            createdAt
        );

        Assert.True(result.IsSuccess);
        var account = result.Value;
        Assert.Equal("admin.ruth", account.UserName);
        Assert.Equal("ADMIN.RUTH", account.NormalizedUserName);
        Assert.Equal("Admin@GraceChurch.org", account.Email);
        Assert.Equal("ADMIN@GRACECHURCH.ORG", account.NormalizedEmail);
        Assert.Equal("Jane Doe", account.DisplayName);
        Assert.Equal("PBKDF2_HASH_SECRET_VALUE", account.PasswordHash);
        Assert.True(account.IsConfirmed);
        Assert.False(account.IsLockedOut);
        Assert.Equal(createdAt.ToUniversalTime(), account.LastPasswordChangedOnUtc);
        var role = Assert.Single(account.Roles);
        Assert.Equal(SecurityRole.AdministratorsRoleId, role.SecurityRoleId);
        Assert.DoesNotContain(
            "PBKDF2_HASH_SECRET_VALUE",
            account.ToString(),
            StringComparison.Ordinal
        );

        var stamp = Guid.CreateVersion7();
        account.SetCreated(createdAt);
        account.SetModified(createdAt.AddMinutes(1));
        account.SetConcurrencyStamp(stamp);
        Assert.Equal(createdAt.ToUniversalTime(), account.CreatedOnUtc);
        Assert.Equal(createdAt.AddMinutes(1).ToUniversalTime(), account.ModifiedOnUtc);
        Assert.Equal(stamp, account.ConcurrencyStamp);
    }

    [Fact]
    public void UserAccount_AddRole_RejectsEmptyAndDuplicateRoles()
    {
        var account = UserAccount
            .CreateAdministrator(
                "admin",
                "admin@example.org",
                "Admin",
                "hash",
                DateTimeOffset.UtcNow
            )
            .Value;

        var emptyResult = account.AddRole(SecurityRoleId.Empty);
        var duplicateResult = account.AddRole(SecurityRole.AdministratorsRoleId);
        var newRoleId = SecurityRoleId.Create();
        var addedResult = account.AddRole(newRoleId);

        Assert.True(emptyResult.IsFailure);
        Assert.Equal(ErrorType.Validation, emptyResult.PrimaryError.Type);
        Assert.True(duplicateResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, duplicateResult.PrimaryError.Type);
        Assert.True(addedResult.IsSuccess);
        Assert.Equal(2, account.Roles.Count);

        var userRole = Assert.Single(account.Roles, r => r.SecurityRoleId == newRoleId);
        var now = DateTimeOffset.UtcNow;
        userRole.SetCreated(now);
        userRole.SetModified(now);
        Assert.Equal(account.Id, userRole.UserAccountId);
        Assert.Equal(now.ToUniversalTime(), userRole.CreatedOnUtc);
        Assert.Equal(now.ToUniversalTime(), userRole.ModifiedOnUtc);
    }

    [Fact]
    public void UserAccount_CreateAdministrator_RejectsInvalidInputs()
    {
        var longText = new string('a', 300);

        var result = UserAccount.CreateAdministrator(
            " ",
            "not-an-email",
            longText,
            "",
            DateTimeOffset.UtcNow
        );

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, static e => e == IdentityErrors.UserNameRequired);
        Assert.Contains(result.Errors, static e => e == IdentityErrors.EmailInvalid);
        Assert.Contains(result.Errors, static e => e == IdentityErrors.DisplayNameTooLong);
        Assert.Contains(result.Errors, static e => e == IdentityErrors.PasswordHashRequired);

        var tooLong = UserAccount.CreateAdministrator(
            longText,
            $"{longText}@example.org",
            " ",
            "hash",
            DateTimeOffset.UtcNow
        );
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.UserNameTooLong);
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.EmailTooLong);
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.DisplayNameRequired);
    }

    [Fact]
    public void SecurityRole_CreateSystemAndCustom_ValidatesInvariants()
    {
        var adminRole = SecurityRole.CreateSystemAdministrators();
        Assert.Equal(SecurityRole.AdministratorsRoleId, adminRole.Id);
        Assert.Equal(SecurityRole.AdministratorsRoleName, adminRole.Name);
        Assert.Equal(SecurityRole.AdministratorsNormalizedName, adminRole.NormalizedName);
        Assert.True(adminRole.IsSystem);

        var stamp = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        adminRole.SetCreated(now);
        adminRole.SetModified(now);
        adminRole.SetConcurrencyStamp(stamp);
        Assert.Equal(now.ToUniversalTime(), adminRole.CreatedOnUtc);
        Assert.Equal(now.ToUniversalTime(), adminRole.ModifiedOnUtc);
        Assert.Equal(stamp, adminRole.ConcurrencyStamp);

        var emptyId = SecurityRole.Create(SecurityRoleId.Empty, "Staff");
        var emptyName = SecurityRole.Create(SecurityRoleId.Create(), " ");
        var tooLongName = SecurityRole.Create(SecurityRoleId.Create(), new string('r', 101));
        var validCustom = SecurityRole.Create(SecurityRoleId.Create(), "  Staff  ");

        Assert.True(emptyId.IsFailure);
        Assert.True(emptyName.IsFailure);
        Assert.True(tooLongName.IsFailure);
        Assert.True(validCustom.IsSuccess);
        Assert.Equal("Staff", validCustom.Value.Name);
        Assert.Equal("STAFF", validCustom.Value.NormalizedName);
        Assert.False(validCustom.Value.IsSystem);
    }

    [Fact]
    public void OrganizationProfile_CompleteInitialOnboarding_SetsLockAndIntegrationEvent()
    {
        var completedAt = DateTimeOffset.UtcNow;

        var result = OrganizationProfile.CompleteInitialOnboarding(
            "  Grace Fellowship ",
            "UTC",
            "  Main Campus ",
            "   ",
            completedAt
        );

        Assert.True(result.IsSuccess);
        var profile = result.Value;
        Assert.Equal(1, profile.SingletonKey);
        Assert.Equal("Grace Fellowship", profile.Name);
        Assert.Equal("UTC", profile.TimeZoneId);
        Assert.Equal("Main Campus", profile.PrimaryCampusName);
        Assert.Null(profile.PrimaryCampusShortCode);
        Assert.True(profile.IsOnboardingCompleted);
        Assert.True(profile.SelfRegistrationLocked);
        Assert.Equal(completedAt.ToUniversalTime(), profile.CompletedOnUtc);

        var evt = Assert.Single(profile.IntegrationEvents);
        var onboarded = Assert.IsType<OrganizationOnboardedIntegrationEvent>(evt);
        Assert.Equal(profile.Id.Value, onboarded.OrganizationId);
        Assert.Equal("Grace Fellowship", onboarded.OrganizationName);
        Assert.Equal("UTC", onboarded.TimeZoneId);
        Assert.Equal(completedAt.ToUniversalTime(), onboarded.CompletedOnUtc);

        var repeatCheck = profile.EnsureCanCompleteOnboarding();
        Assert.True(repeatCheck.IsFailure);
        Assert.Equal(ErrorType.Conflict, repeatCheck.PrimaryError.Type);

        profile.ClearIntegrationEvents();
        Assert.Empty(profile.IntegrationEvents);

        var stamp = Guid.CreateVersion7();
        profile.SetCreated(completedAt);
        profile.SetModified(completedAt);
        profile.SetConcurrencyStamp(stamp);
        Assert.Equal(completedAt.ToUniversalTime(), profile.CreatedOnUtc);
        Assert.Equal(completedAt.ToUniversalTime(), profile.ModifiedOnUtc);
        Assert.Equal(stamp, profile.ConcurrencyStamp);
    }

    [Fact]
    public void OrganizationProfile_CompleteInitialOnboarding_RejectsInvalidInputs()
    {
        var longText = new string('x', 250);

        var invalid = OrganizationProfile.CompleteInitialOnboarding(
            " ",
            "Invalid/Zone_Not_Real",
            " ",
            longText,
            DateTimeOffset.UtcNow
        );

        Assert.True(invalid.IsFailure);
        Assert.Contains(invalid.Errors, static e => e == IdentityErrors.OrganizationNameRequired);
        Assert.Contains(invalid.Errors, static e => e == IdentityErrors.TimeZoneInvalid);
        Assert.Contains(invalid.Errors, static e => e == IdentityErrors.PrimaryCampusNameRequired);
        Assert.Contains(invalid.Errors, static e =>
            e == IdentityErrors.PrimaryCampusShortCodeTooLong
        );

        var tooLong = OrganizationProfile.CompleteInitialOnboarding(
            longText,
            new string('z', 60),
            longText,
            "MAIN",
            DateTimeOffset.UtcNow
        );
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.OrganizationNameTooLong);
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.TimeZoneInvalid);
        Assert.Contains(tooLong.Errors, static e => e == IdentityErrors.PrimaryCampusNameTooLong);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Short1!", false)]
    [InlineData("alllowercase1!", false)]
    [InlineData("ALLUPPERCASE1!", false)]
    [InlineData("NoDigitsHere!!", false)]
    [InlineData("NoSymbols12345", false)]
    [InlineData("ValidPass#2026!", true)]
    public void OnboardingPolicy_ValidatePasswordComplexity_EnforcesRules(
        string? password,
        bool expectedSuccess
    )
    {
        var result = OnboardingPolicy.ValidatePasswordComplexity(password);
        Assert.Equal(expectedSuccess, result.IsSuccess);
    }

    [Fact]
    public void OnboardingPolicy_ValidatePasswordComplexity_RejectsTooLongPassword()
    {
        var tooLong = new string('A', 125) + "a1!a1!";
        var result = OnboardingPolicy.ValidatePasswordComplexity(tooLong);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.PasswordTooLong, result.PrimaryError);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(" ", false)]
    [InlineData("plainaddress", false)]
    [InlineData("admin@localhost", false)]
    [InlineData("admin\0@example.org", false)]
    [InlineData("admin@example\0.org", false)]
    [InlineData("admin\n@example.org", false)]
    [InlineData("admin@example.org", true)]
    public void OnboardingPolicy_ValidateEmail_EnforcesValidDomainEmail(
        string? email,
        bool expectedSuccess
    )
    {
        var result = OnboardingPolicy.ValidateEmail(email);
        Assert.Equal(expectedSuccess, result.IsSuccess);
    }

    [Theory]
    [InlineData("Org\0Name")]
    [InlineData("\0")]
    [InlineData("Org\nName")]
    [InlineData("Org\u0007Name")]
    public void OnboardingPolicy_ValidateRequiredText_RejectsNulBytesAndControlCharacters(
        string value
    )
    {
        var result = OnboardingPolicy.ValidateRequiredText(
            value,
            OnboardingPolicy.OrganizationNameMaxLength,
            IdentityErrors.OrganizationNameRequired,
            IdentityErrors.OrganizationNameTooLong
        );

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidControlCharacters, result.PrimaryError);
    }

    [Theory]
    [InlineData("SC\01")]
    [InlineData("\0")]
    [InlineData("SC\n1")]
    public void OnboardingPolicy_ValidateOptionalText_RejectsNulBytesAndControlCharacters(
        string value
    )
    {
        var result = OnboardingPolicy.ValidateOptionalText(
            value,
            OnboardingPolicy.CampusShortCodeMaxLength,
            IdentityErrors.PrimaryCampusShortCodeTooLong
        );

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidControlCharacters, result.PrimaryError);
    }

    [Theory]
    [InlineData("UTC\0")]
    [InlineData("\0UTC")]
    [InlineData("America/\nNew_York")]
    [InlineData("UTC\u0007")]
    public void OnboardingPolicy_ValidateTimeZoneId_RejectsNulBytesAndControlCharacters(
        string timeZoneId
    )
    {
        var result = OnboardingPolicy.ValidateTimeZoneId(timeZoneId);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.TimeZoneInvalid, result.PrimaryError);
    }

    [Fact]
    public void Aggregates_RejectNulBytesInTextFields()
    {
        var orgResult = OrganizationProfile.CompleteInitialOnboarding(
            "Org\0Name",
            "UTC",
            "Main\0Campus",
            "SC\01",
            DateTimeOffset.UtcNow
        );
        var userResult = UserAccount.CreateAdministrator(
            "admin\0evil",
            "admin\0@example.org",
            "Admin\0Name",
            "hash",
            DateTimeOffset.UtcNow
        );
        var roleResult = SecurityRole.Create(SecurityRoleId.Create(), "Staff\0Role");

        Assert.True(orgResult.IsFailure);
        Assert.Contains(orgResult.Errors, static e => e == IdentityErrors.InvalidControlCharacters);
        Assert.True(userResult.IsFailure);
        Assert.Contains(userResult.Errors, static e =>
            e == IdentityErrors.InvalidControlCharacters
        );
        Assert.Contains(userResult.Errors, static e => e == IdentityErrors.EmailInvalid);
        Assert.True(roleResult.IsFailure);
        Assert.Equal(IdentityErrors.InvalidControlCharacters, roleResult.PrimaryError);
    }
}
