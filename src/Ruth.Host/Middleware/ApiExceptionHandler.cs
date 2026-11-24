using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ruth.Host.Middleware;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ApiExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var mapping = ProblemDetailsMapper.MapException(exception, environment.IsDevelopment());

        if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            HostLog.UnhandledException(
                logger,
                exception,
                mapping.StatusCode,
                httpContext.Request.Method,
                httpContext.Request.Path
            );
        }
        else
        {
            HostLog.ClientExceptionHandled(
                logger,
                exception,
                mapping.StatusCode,
                mapping.Title,
                httpContext.Request.Method,
                httpContext.Request.Path
            );
        }

        var problemDetails = new ProblemDetails
        {
            Status = mapping.StatusCode,
            Title = mapping.Title,
            Detail = mapping.Detail,
            Type = mapping.TypeReference,
            Instance = httpContext.Request.Path,
        };

        await ProblemDetailsResponseWriter.WriteAsync(
            httpContext,
            problemDetailsService,
            problemDetails,
            environment.IsDevelopment() ? exception : null,
            cancellationToken
        );
        return true;
    }
}
