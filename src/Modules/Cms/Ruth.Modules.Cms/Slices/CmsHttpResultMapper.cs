using Microsoft.AspNetCore.Http;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices;

internal static class CmsHttpResultMapper
{
    public static Microsoft.AspNetCore.Http.IResult ToHttpResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.PrimaryError);
    }

    public static Microsoft.AspNetCore.Http.IResult ToProblem(DomainError error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure or ErrorType.Unexpected or _ =>
                StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: status,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["errorCode"] = error.Code,
            }
        );
    }
}
