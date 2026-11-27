using Ruth.Modules.Tenancy.Domain;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Tenancy.Tests;

public sealed class StripeConnectAccountDomainTests
{
    [Fact]
    public void StripeConnectAccount_Lifecycle_Validates_AccountId_And_Synchronizes_Capabilities()
    {
        var now = new DateTimeOffset(2026, 11, 26, 14, 0, 0, TimeSpan.Zero);
        Assert.True(
            StripeConnectAccount
                .Create(
                    TenantId.Empty,
                    "acct_123",
                    isMockAccount: true,
                    chargesEnabled: false,
                    payoutsEnabled: false,
                    detailsSubmitted: false,
                    onboardingUrl: null,
                    onboardingExpiresAtUtc: null,
                    now
                )
                .IsFailure
        );
        Assert.True(
            StripeConnectAccount
                .Create(
                    TenantId.Create(),
                    externalAccountId: "",
                    isMockAccount: true,
                    chargesEnabled: false,
                    payoutsEnabled: false,
                    detailsSubmitted: false,
                    onboardingUrl: null,
                    onboardingExpiresAtUtc: null,
                    now
                )
                .IsFailure
        );

        var created = StripeConnectAccount
            .Create(
                TenantId.Create(),
                "acct_mock_555",
                isMockAccount: true,
                chargesEnabled: false,
                payoutsEnabled: false,
                detailsSubmitted: false,
                "https://connect.example/onboard",
                now.AddMinutes(30),
                now
            )
            .Value;

        Assert.True(
            created
                .RecordOnboardingSession(
                    externalAccountId: " ",
                    onboardingUrl: null,
                    now.AddMinutes(15),
                    isMockAccount: true,
                    now
                )
                .IsFailure
        );
        Assert.True(
            created
                .RecordOnboardingSession(
                    "acct_mock_777",
                    "https://connect.example/refresh",
                    now.AddMinutes(45),
                    isMockAccount: true,
                    now
                )
                .IsSuccess
        );
        Assert.Equal("acct_mock_777", created.ExternalAccountId);

        created.SynchronizeCapabilities(
            chargesEnabled: true,
            payoutsEnabled: true,
            detailsSubmitted: true,
            now.AddMinutes(5)
        );
        Assert.True(created.ChargesEnabled && created.PayoutsEnabled && created.DetailsSubmitted);

        var stamp = Guid.CreateVersion7();
        created.SetCreated(now);
        created.SetModified(now.AddMinutes(6));
        created.SetConcurrencyStamp(stamp);
        Assert.Equal(stamp, created.ConcurrencyStamp);
    }
}
