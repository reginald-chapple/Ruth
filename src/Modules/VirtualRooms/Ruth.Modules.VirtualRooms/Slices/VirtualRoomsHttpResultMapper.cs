using Microsoft.AspNetCore.Http;
using Ruth.SharedKernel.Results;
using HttpIResult = Microsoft.AspNetCore.Http.IResult;

namespace Ruth.Modules.VirtualRooms.Slices;

internal static class VirtualRoomsHttpResultMapper
{
    public static HttpIResult ToHttpResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.PrimaryError);
    }

    public static HttpIResult ToProblem(DomainError error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure or ErrorType.Unexpected or _ =>
                StatusCodes.Status500InternalServerError,
        };
        return Results.Problem(title: error.Code, detail: error.Description, statusCode: status);
    }
}
