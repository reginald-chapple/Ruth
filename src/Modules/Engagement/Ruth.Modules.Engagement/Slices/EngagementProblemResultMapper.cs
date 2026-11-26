using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices;

internal static class EngagementProblemResultMapper
{
    public static ProblemHttpResult ToProblemResult(Result result, HttpContext httpContext)
    {
        var primary = result.PrimaryError;
        var (status, title) = primary.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.Failure => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
            ErrorType.Unexpected => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error"
            ),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        var groupedErrors = result
            .Errors.GroupBy(static e => e.Code, StringComparer.Ordinal)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.Description).ToArray(),
                StringComparer.Ordinal
            );

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["errorCode"] = primary.Code,
            ["errors"] = groupedErrors,
        };

        return TypedResults.Problem(
            detail: primary.Description,
            instance: httpContext.Request.Path.Value,
            statusCode: status,
            title: title,
            extensions: extensions
        );
    }
}
