using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Ruth.SharedKernel.Results;
using DomainResult = Ruth.SharedKernel.Results.IResult;

namespace Ruth.Modules.Tenancy.Slices.Endpoints;

internal static class TenancyProblemResultMapper
{
    public static ProblemHttpResult ToProblem(DomainResult result, HttpContext httpContext)
    {
        var primary =
            result.Errors.Count > 0
                ? result.Errors[0]
                : DomainError.Unexpected("Tenancy.Error", "An unexpected error occurred.");
        var statusCode = primary.Type switch
        {
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            title: primary.Code,
            detail: primary.Description,
            statusCode: statusCode,
            instance: httpContext.Request.Path
        );
    }
}
