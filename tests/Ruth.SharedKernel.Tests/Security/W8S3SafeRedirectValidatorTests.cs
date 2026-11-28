using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.SharedKernel.Tests.Security;

public sealed class W8S3SafeRedirectValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/dashboard")]
    [InlineData("/portal/giving?fund=general")]
    public void IsSafeLocalReturnPath_AcceptsSafeRootRelativePaths(string? returnUrl)
    {
        Assert.True(SafeRedirectValidator.IsSafeLocalReturnPath(returnUrl));
    }

    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example/steal")]
    [InlineData(@"/\evil.example/steal")]
    [InlineData("/%2fevil.example/steal")]
    [InlineData("/%5cevil.example/steal")]
    [InlineData("/path\\other")]
    [InlineData("/user@evil.example")]
    [InlineData("/path with space")]
    [InlineData("/path\r\nHeader: injected")]
    [InlineData("/redirect?next=https://evil.example")]
    [InlineData("relative/no-leading-slash")]
    public void IsSafeLocalReturnPath_RejectsOpenRedirectPayloads(string returnUrl)
    {
        Assert.False(SafeRedirectValidator.IsSafeLocalReturnPath(returnUrl));
    }

    [Fact]
    public void IsSafeLocalReturnPath_RejectsOverlengthPaths()
    {
        var tooLong = "/" + new string('a', 1025);
        Assert.False(SafeRedirectValidator.IsSafeLocalReturnPath(tooLong));
    }

    [Theory]
    [InlineData("https://portal.charity.org/oidc/callback")]
    [InlineData("http://localhost:5173/auth/callback")]
    [InlineData("http://127.0.0.1:8080/callback")]
    [InlineData("http://[::1]:8080/callback")]
    [InlineData("org.charity.mobile:/oauth/callback")]
    [InlineData("/api/auth/oidc/google/callback")]
    public void IsSafeRegisteredRedirectUri_AcceptsHttpsLoopbackLocalAndValidCustomSchemes(
        string redirectUri
    )
    {
        Assert.True(SafeRedirectValidator.IsSafeRegisteredRedirectUri(redirectUri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://portal.charity.org/oidc/callback")]
    [InlineData("https://*.charity.org/oidc/callback")]
    [InlineData("https://portal.charity.org/oidc/callback#frag")]
    [InlineData("https://user@portal.charity.org/oidc/callback")]
    [InlineData("https://portal.charity.org/oidc/../admin")]
    [InlineData("https://portal.charity.org/oidc/%2e%2e/admin")]
    [InlineData("/oidc/../admin")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ws://localhost:5000/ws")]
    [InlineData("wss://portal.charity.org/ws")]
    [InlineData("a:/too-short-scheme")]
    [InlineData("bad_scheme:/underscore-disallowed")]
    public void IsSafeRegisteredRedirectUri_RejectsUnsafeRedirectUris(string? redirectUri)
    {
        Assert.False(SafeRedirectValidator.IsSafeRegisteredRedirectUri(redirectUri));
    }

    [Fact]
    public void MatchesExactRedirectUri_EnforcesStrictOrdinalEquality()
    {
        const string registered = "https://portal.charity.org/oidc/callback";

        Assert.True(SafeRedirectValidator.MatchesExactRedirectUri(registered, registered));
        Assert.False(
            SafeRedirectValidator.MatchesExactRedirectUri(
                registered,
                "https://PORTAL.charity.org/oidc/callback"
            )
        );
        Assert.False(
            SafeRedirectValidator.MatchesExactRedirectUri(
                registered,
                "https://portal.charity.org/oidc/callback/"
            )
        );
        Assert.False(
            SafeRedirectValidator.MatchesExactRedirectUri(
                registered,
                "https://portal.charity.org/oidc/callback?extra=1"
            )
        );
        Assert.False(
            SafeRedirectValidator.MatchesExactRedirectUri(registered, candidateRedirectUri: null)
        );
        Assert.False(SafeRedirectValidator.MatchesExactRedirectUri("", registered));
    }
}
