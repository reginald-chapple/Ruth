using Microsoft.AspNetCore.Http.HttpResults;

namespace Ruth.Host.Endpoints;

/// <summary>Maps host liveness and operational probe endpoints.</summary>
public static class HealthEndpoints
{
    /// <summary>Registers health probe endpoints on the application route builder.</summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        _ = app.MapGet("/health/live", static Ok<LivenessResponse> () =>
                TypedResults.Ok(new LivenessResponse("Healthy"))
            )
            .WithName("GetLiveness")
            .WithSummary("Get host liveness status")
            .WithDescription("Returns 200 OK when the Ruth.Host process is alive.");

        return app;
    }
}
