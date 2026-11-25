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

    [Fact]
    public async Task ApiPipeline_AcceptTextHtml_FallsBackToProblemJsonFor503And400()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Production",
            assumeOnboarded: false,
            configureServices: static services =>
                _ = services.Replace(
                    ServiceDescriptor.Singleton<IOnboardingStateReader>(
                        new CompletedOnboardingStateReaderStub(isOnboardingRequired: true)
                    )
                )
        );
        using var client = factory.CreateNoRedirectClient();

        using var gatedReq = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/api/unknown", UriKind.Relative)
        );
        gatedReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        using var gatedResp = await client.SendAsync(gatedReq);
        var gatedBody = await gatedResp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, gatedResp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            gatedResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using (var gatedDoc = JsonDocument.Parse(gatedBody))
        {
            Assert.Equal(503, gatedDoc.RootElement.GetProperty("status").GetInt32());
            Assert.Equal(
                "Onboarding.Required",
                gatedDoc.RootElement.GetProperty("errorCode").GetString()
            );
        }

        using var badJsonReq = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/onboarding", UriKind.Relative)
        )
        {
            Content = new StringContent("{ bad json", Encoding.UTF8, "application/json"),
        };
        badJsonReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        using var badJsonResp = await client.SendAsync(badJsonReq);
        var badJsonBody = await badJsonResp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, badJsonResp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            badJsonResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using var badJsonDoc = JsonDocument.Parse(badJsonBody);
        Assert.Equal(400, badJsonDoc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ApiPipeline_ReturnsProblemDetails_For415_405_And_404Responses()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Production",
            assumeOnboarded: true
        );
        using var client = factory.CreateNoRedirectClient();

        using var unsupportedMediaResp = await client.PostAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new StringContent("plain text", Encoding.UTF8, "text/plain")
        );
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupportedMediaResp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            unsupportedMediaResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using (
            var doc415 = JsonDocument.Parse(await unsupportedMediaResp.Content.ReadAsStringAsync())
        )
        {
            Assert.Equal(415, doc415.RootElement.GetProperty("status").GetInt32());
        }

        using var methodNotAllowedResp = await client.PutAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new StringContent("{}", Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.MethodNotAllowed, methodNotAllowedResp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            methodNotAllowedResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using (
            var doc405 = JsonDocument.Parse(await methodNotAllowedResp.Content.ReadAsStringAsync())
        )
        {
            Assert.Equal(405, doc405.RootElement.GetProperty("status").GetInt32());
        }

        using var unknownPostResp = await client.PostAsync(
            new Uri("/api/unknown", UriKind.Relative),
            new StringContent("{}", Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.MethodNotAllowed, unknownPostResp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            unknownPostResp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using var docUnknownPost = JsonDocument.Parse(
            await unknownPostResp.Content.ReadAsStringAsync()
        );
        Assert.Equal(405, docUnknownPost.RootElement.GetProperty("status").GetInt32());
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
