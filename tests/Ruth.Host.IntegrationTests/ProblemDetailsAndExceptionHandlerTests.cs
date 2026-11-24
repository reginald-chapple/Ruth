using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Host.Middleware;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Results;
using Xunit;
using DomainResult = Ruth.SharedKernel.Results.Result;

namespace Ruth.Host.IntegrationTests;

public sealed class ProblemDetailsAndExceptionHandlerTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400, "Validation Failed")]
    [InlineData(ErrorType.Unauthorized, 401, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, 403, "Forbidden")]
    [InlineData(ErrorType.NotFound, 404, "Not Found")]
    [InlineData(ErrorType.Conflict, 409, "Conflict")]
    [InlineData(ErrorType.Failure, 422, "Unprocessable Entity")]
    [InlineData(ErrorType.Unexpected, 500, "Internal Server Error")]
    public void ProblemDetailsMapper_MapsAllDomainErrorTypes(
        ErrorType errorType,
        int expectedStatus,
        string expectedTitle
    )
    {
        var error = new DomainError("Test.Code", "Test failure description.", errorType);
        var result = DomainResult.Failure(error);

        var problem = ProblemDetailsMapper.ToProblemDetails(result, "/api/test");

        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal("Test failure description.", problem.Detail);
        Assert.Equal("/api/test", problem.Instance);
        Assert.Equal("Test.Code", problem.Extensions["errorCode"]);
    }

    [Fact]
    public void ProblemDetailsMapper_IncludesGroupedErrorsForMultipleValidationFailures()
    {
        var result = DomainResult.Failure([
            DomainError.Validation("Email", "Email is required."),
            DomainError.Validation("Email", "Email format is invalid."),
            DomainError.Validation("Name", "Name is required."),
        ]);

        var problem = ProblemDetailsMapper.ToProblemDetails(result, "/api/users");

        Assert.Equal(400, problem.Status);
        var errors = Assert.IsType<Dictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(2, errors["Email"].Length);
        _ = Assert.Single(errors["Name"]);
    }

    [Theory]
    [InlineData("badhttp", HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData("json", HttpStatusCode.BadRequest, "Invalid JSON")]
    [InlineData("validation", HttpStatusCode.BadRequest, "Validation Failed")]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData("notfound", HttpStatusCode.NotFound, "Not Found")]
    [InlineData("concurrency", HttpStatusCode.Conflict, "Concurrency Conflict")]
    [InlineData("invalidop", HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData("unhandled", HttpStatusCode.InternalServerError, "Internal Server Error")]
    public async Task ApiExceptionHandler_MapsExceptionsToRfc7807ProblemDetails(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedTitle
    )
    {
        await using var factory = CreateExceptionTestFactory("Production");
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(
            new Uri($"/_test/throw/{kind}", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );

        using var doc = JsonDocument.Parse(body);
        Assert.Equal((int)expectedStatus, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, doc.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("internal-secret-marker", body, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("badhttp", HttpStatusCode.BadRequest, "Bad Request", true)]
    [InlineData("json", HttpStatusCode.BadRequest, "Invalid JSON", false)]
    [InlineData("validation", HttpStatusCode.BadRequest, "Validation Failed", true)]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "Unauthorized", false)]
    [InlineData("notfound", HttpStatusCode.NotFound, "Not Found", false)]
    [InlineData("concurrency", HttpStatusCode.Conflict, "Concurrency Conflict", false)]
    [InlineData("invalidop", HttpStatusCode.InternalServerError, "Internal Server Error", true)]
    [InlineData("unhandled", HttpStatusCode.InternalServerError, "Internal Server Error", true)]
    public async Task ApiExceptionHandler_InDevelopment_IncludesDetailOnlyForAllowedExceptions(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        bool expectsMessageInDetail
    )
    {
        await using var factory = CreateExceptionTestFactory("Development");
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(
            new Uri($"/_test/throw/{kind}", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(expectedTitle, doc.RootElement.GetProperty("title").GetString());
        var detail = doc.RootElement.GetProperty("detail").GetString();
        Assert.NotNull(detail);

        if (expectsMessageInDetail)
        {
            Assert.Contains("internal-secret-marker", detail, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("internal-secret-marker", detail, StringComparison.Ordinal);
        }
    }

    private static HostWebApplicationFactory CreateExceptionTestFactory(string environment)
    {
        return HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: environment,
            testRouteInterceptor: static (context, next) =>
            {
                if (
                    !context.Request.Path.StartsWithSegments(
                        "/_test/throw",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return next();
                }

                var kind = context.Request.Path.Value?.Split('/').LastOrDefault();
                throw kind switch
                {
                    "badhttp" => new BadHttpRequestException(
                        "Malformed request internal-secret-marker"
                    ),
                    "json" => new JsonException("Malformed JSON internal-secret-marker"),
                    "validation" => new ValidationException("Invalid field internal-secret-marker"),
                    "unauthorized" => new UnauthorizedAccessException(
                        "Denied internal-secret-marker"
                    ),
                    "notfound" => new KeyNotFoundException("Missing internal-secret-marker"),
                    "concurrency" => new DbUpdateConcurrencyException(
                        "Conflict internal-secret-marker"
                    ),
                    "invalidop" => new InvalidOperationException("Bug internal-secret-marker"),
                    _ => new InvalidTimeZoneException("Unhandled internal-secret-marker"),
                };
            }
        );
    }
}
