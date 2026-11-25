using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Ruth.SharedKernel.Results;
using DomainResult = Ruth.SharedKernel.Results.Result;

namespace Ruth.Modules.Crm.Slices;

internal static class CrmProblemResultMapper
{
    public static ProblemHttpResult ToProblemResult(DomainResult result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        var primary = result.PrimaryError;
        var (statusCode, title, typeReference) = MapErrorType(primary.Type);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = primary.Description,
            Type = typeReference,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["errorCode"] = primary.Code;
        return TypedResults.Problem(problem);
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
            ErrorType.Unexpected or _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            ),
        };
    }
}
