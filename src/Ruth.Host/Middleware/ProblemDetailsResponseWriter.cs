using System.Collections.Frozen;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ruth.Host.Middleware;

internal static class ProblemDetailsResponseWriter
{
    private const string DefaultTypeReference = "https://tools.ietf.org/html/rfc9110#section-15.5";

    private static readonly FrozenDictionary<int, string> _typeReferences = new Dictionary<
        int,
        string
    >
    {
        [StatusCodes.Status400BadRequest] = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        [StatusCodes.Status401Unauthorized] = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        [StatusCodes.Status403Forbidden] = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        [StatusCodes.Status404NotFound] = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        [StatusCodes.Status405MethodNotAllowed] =
            "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        [StatusCodes.Status406NotAcceptable] = "https://tools.ietf.org/html/rfc9110#section-15.5.7",
        [StatusCodes.Status409Conflict] = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        [StatusCodes.Status413PayloadTooLarge] =
            "https://tools.ietf.org/html/rfc9110#section-15.5.14",
        [StatusCodes.Status415UnsupportedMediaType] =
            "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        [StatusCodes.Status422UnprocessableEntity] =
            "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        [StatusCodes.Status429TooManyRequests] = "https://tools.ietf.org/html/rfc6585#section-4",
        [StatusCodes.Status503ServiceUnavailable] =
            "https://tools.ietf.org/html/rfc9110#section-15.6.4",
    }.ToFrozenDictionary();
    public static async ValueTask WriteAsync(
        HttpContext httpContext,
        IProblemDetailsService? problemDetailsService,
        ProblemDetails problemDetails,
        Exception? exception = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problemDetails);

        if (httpContext.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        {
            statusCodePages.Enabled = false;
        }

        if (problemDetails.Status is { } statusCode)
        {
            httpContext.Response.StatusCode = statusCode;
        }

        if (problemDetailsService is not null)
        {
            var wrote = await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = problemDetails,
                }
            );
            if (wrote)
            {
                return;
            }
        }

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        _ = problemDetails.Extensions.TryAdd(
            "traceId",
            Activity.Current?.Id ?? httpContext.TraceIdentifier
        );

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken
        );
    }

    public static Task WriteApiStatusCodeProblemAsync(StatusCodeContext statusCodeContext)
    {
        ArgumentNullException.ThrowIfNull(statusCodeContext);

        var httpContext = statusCodeContext.HttpContext;
        if (httpContext.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        var statusCode = httpContext.Response.StatusCode;
        var reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = string.IsNullOrWhiteSpace(reasonPhrase) ? "HTTP Error" : reasonPhrase,
            Type = GetTypeReference(statusCode),
            Instance = httpContext.Request.Path,
        };

        var problemDetailsService =
            httpContext.RequestServices.GetService<IProblemDetailsService>();

        return WriteAsync(
                httpContext,
                problemDetailsService,
                problemDetails,
                cancellationToken: httpContext.RequestAborted
            )
            .AsTask();
    }

    private static string GetTypeReference(int statusCode)
    {
        return _typeReferences.GetValueOrDefault(statusCode, DefaultTypeReference);
    }
}
