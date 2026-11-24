using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Slices;

public sealed class OnboardingSliceUnitTests
{
    [Fact]
    public async Task CompleteOnboardingCommandValidator_AcceptsValidCommandAndRejectsInvalidFields()
    {
        var validator = new CompleteOnboardingCommandValidator();

        var valid = new CompleteOnboardingCommand(
            "Grace Fellowship",
            "UTC",
            "Main Campus",
            "MAIN",
            "admin",
            "admin@grace.org",
            "Grace Admin",
            "StrongPass#2026!"
        );
        var validErrors = await validator.ValidateAsync(valid);
        Assert.Empty(validErrors);

        var oversizedShortCode = new string('C', 51);
        var invalid = new CompleteOnboardingCommand(
            "",
            "Not/A/Zone",
            "",
            oversizedShortCode,
            "",
            "bad-email",
            "",
            "weak"
        );
        var invalidErrors = await validator.ValidateAsync(invalid);
        Assert.Equal(8, invalidErrors.Count);
    }

    [Fact]
    public async Task CompleteOnboardingCommandValidator_RejectsNulBytesAcrossAllTextFields()
    {
        var validator = new CompleteOnboardingCommandValidator();
        var command = new CompleteOnboardingCommand(
            "Org\0Name",
            "UTC\0",
            "Main\0Campus",
            "SC\01",
            "admin\0evil",
            "admin\0@grace.org",
            "Admin\0Name",
            "StrongPass#2026!"
        );

        var errors = await validator.ValidateAsync(command);

        Assert.Equal(7, errors.Count);
        Assert.All(errors, static e => Assert.Equal(ErrorType.Validation, e.Type));
    }

    [Fact]
    public async Task GetOnboardingStatusQueryHandler_ReturnsReaderStatus()
    {
        var expected = new OnboardingStatusResponse(
            IsOnboardingRequired: false,
            IsSelfRegistrationLocked: true,
            "Grace Fellowship"
        );
        var handler = new GetOnboardingStatusQueryHandler(new FakeStateReader(expected));

        var result = await handler.HandleAsync(new GetOnboardingStatusQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest, "Validation Failed")]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden, "Forbidden")]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, "Not Found")]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, "Conflict")]
    [InlineData(
        ErrorType.Failure,
        StatusCodes.Status422UnprocessableEntity,
        "Unprocessable Entity"
    )]
    [InlineData(
        ErrorType.Unexpected,
        StatusCodes.Status500InternalServerError,
        "Internal Server Error"
    )]
    public void OnboardingProblemResultMapper_MapsAllErrorTypes(
        ErrorType errorType,
        int expectedStatus,
        string expectedTitle
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/onboarding";

        var result = Result.Failure([
            new DomainError("Code.One", "First failure.", errorType),
            new DomainError("Code.Two", "Second failure.", errorType),
        ]);

        var mapped = Assert.IsType<ProblemHttpResult>(
            OnboardingProblemResultMapper.ToProblemResult(result, httpContext)
        );

        Assert.Equal(expectedStatus, mapped.StatusCode);
        Assert.Equal(expectedTitle, mapped.ProblemDetails.Title);
        Assert.Equal("/api/onboarding", mapped.ProblemDetails.Instance);
        Assert.Equal("Code.One", mapped.ProblemDetails.Extensions["errorCode"]);
        var errors = Assert.IsType<Dictionary<string, string[]>>(
            mapped.ProblemDetails.Extensions["errors"]
        );
        Assert.Equal(2, errors.Count);
    }

    private sealed class FakeStateReader(OnboardingStatusResponse response) : IOnboardingStateReader
    {
        public ValueTask<OnboardingStatusResponse> GetStatusAsync(
            CancellationToken cancellationToken = default
        )
        {
            return ValueTask.FromResult(response);
        }

        public void InvalidateCache() { }
    }
}
