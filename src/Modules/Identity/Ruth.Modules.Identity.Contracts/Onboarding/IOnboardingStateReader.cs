namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>Reads and caches the immutable post-completion onboarding state.</summary>
public interface IOnboardingStateReader
{
    /// <summary>Gets the current onboarding status from cache (if permanently locked) or persistence.</summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The current <see cref="OnboardingStatusResponse" />.</returns>
    ValueTask<OnboardingStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Invalidates any cached onboarding snapshot.</summary>
    void InvalidateCache();
}
