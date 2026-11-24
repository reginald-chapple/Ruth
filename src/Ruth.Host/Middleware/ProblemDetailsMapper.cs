using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ruth.SharedKernel.Results;
using DomainResult = Ruth.SharedKernel.Results.Result;
using HttpIResult = Microsoft.AspNetCore.Http.IResult;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Ruth.Host.Middleware;

internal static class ProblemDetailsMapper
{
    public static ProblemDetails ToProblemDetails(DomainResult result, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new ArgumentException(
                "Cannot map a successful result to ProblemDetails.",
                nameof(result)
            );
        }

        var primary = result.PrimaryError;
        var (statusCode, title, typeReference) = MapErrorType(primary.Type);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = primary.Description,
            Type = typeReference,
            Instance = instance,
        };

        problem.Extensions["errorCode"] = primary.Code;

        if (result.Errors.Count > 1)
        {
            problem.Extensions["errors"] = BuildErrorsDictionary(result.Errors);
        }

        return problem;
    }

    public static HttpIResult ToHttpResult(DomainResult result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var problem = ToProblemDetails(result, httpContext.Request.Path);
        return HttpResults.Problem(problem);
    }

    public static ExceptionProblemMapping MapException(Exception exception, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title, productionDetail, typeReference, exposeMessageInDevelopment) =
            TryMapInputException(exception)
            ?? TryMapResourceException(exception)
            ?? (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                true
            );

        var detail =
            isDevelopment && exposeMessageInDevelopment ? exception.Message : productionDetail;

        return new ExceptionProblemMapping(statusCode, title, detail, typeReference);
    }

    private static (
        int StatusCode,
        string Title,
        string ProductionDetail,
        string TypeReference,
        bool ExposeMessageInDevelopment
    )? TryMapInputException(Exception exception)
    {
        return exception switch
        {
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                "Bad Request",
                "Invalid HTTP request.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                true
            ),
            JsonException => (
                StatusCodes.Status400BadRequest,
                "Invalid JSON",
                "The request body contains malformed or incompatible JSON.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                false
            ),
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more validation errors occurred.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                true
            ),
            _ => null,
        };
    }

    private static (
        int StatusCode,
        string Title,
        string ProductionDetail,
        string TypeReference,
        bool ExposeMessageInDevelopment
    )? TryMapResourceException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                false
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "The requested resource was not found.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                false
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "The resource was modified by another operation. Please retry.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                false
            ),
            _ => null,
        };
    }

    private static (int StatusCode, string Title, string TypeReference) MapErrorType(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            ),
            ErrorType.Unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            ),
            ErrorType.Forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            ),
            ErrorType.NotFound => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            ),
            ErrorType.Conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"
            ),
            ErrorType.Failure => (
                StatusCodes.Status422UnprocessableEntity,
                "Unprocessable Entity",
                "https://tools.ietf.org/html/rfc9110#section-15.5.21"
            ),
            ErrorType.Unexpected => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            ),
        };
    }

    private static Dictionary<string, string[]> BuildErrorsDictionary(
        IReadOnlyList<DomainError> errors
    )
    {
        var grouped = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var group in errors.GroupBy(static e => e.Code, StringComparer.Ordinal))
        {
            grouped[group.Key] = [.. group.Select(static e => e.Description)];
        }

        return grouped;
    }

    internal sealed record ExceptionProblemMapping(
        int StatusCode,
        string Title,
        string Detail,
        string TypeReference
    );
}
