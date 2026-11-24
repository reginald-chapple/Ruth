using Ruth.Modules.Identity.Contracts.Onboarding;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal sealed class CompletedOnboardingStateReaderStub(bool isOnboardingRequired = false)
    : IOnboardingStateReader
{
    private readonly OnboardingStatusResponse _response = new(
        IsOnboardingRequired: isOnboardingRequired,
        IsSelfRegistrationLocked: !isOnboardingRequired,
        OrganizationName: isOnboardingRequired ? null : "Test Organization"
    );

    public int InvalidateCount { get; private set; }

    public ValueTask<OnboardingStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(_response);
    }

    public void InvalidateCache()
    {
        InvalidateCount++;
    }
}
