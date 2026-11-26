using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal static class ApplePassKitWebServiceEndpoints
{
    public static IEndpointRouteBuilder MapApplePassKitWebServiceEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var v1 = endpoints
            .MapGroup("/api/checkin/wallet/v1")
            .WithTags("CheckIn - Apple PassKit WebService v1")
            .AllowAnonymous();

        MapRegisterRoute(v1);
        MapListRoute(v1);
        MapUnregisterRoute(v1);
        MapPassDeliveryAndLogRoutes(v1);

        return endpoints;
    }

    private static void MapRegisterRoute(RouteGroupBuilder v1)
    {
        _ = v1.MapPost(
                "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
                static async Task<HttpResult> (
                    string deviceLibraryIdentifier,
                    string passTypeIdentifier,
                    string serialNumber,
                    PassKitDeviceRegistrationBody body,
                    ICommandHandler<RegisterPassKitDeviceCommand, bool> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var auth = WalletPassSecurityTokens.ExtractApplePassHeaderToken(
                        ctx.Request.Headers.Authorization
                    );
                    var res = await handler.HandleAsync(
                        new RegisterPassKitDeviceCommand(
                            deviceLibraryIdentifier,
                            passTypeIdentifier,
                            serialNumber,
                            body.PushToken,
                            auth
                        ),
                        ct
                    );
                    return res.IsFailure ? ToPassKitStatusResult(res, ctx)
                        : res.Value ? Results.StatusCode(StatusCodes.Status201Created)
                        : Results.Ok();
                }
            )
            .WithName("PassKitRegisterDevice");
    }

    private static void MapListRoute(RouteGroupBuilder v1)
    {
        _ = v1.MapGet(
                "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}",
                static async Task<HttpResult> (
                    string deviceLibraryIdentifier,
                    string passTypeIdentifier,
                    [FromQuery] string? passesUpdatedSince,
                    IQueryHandler<
                        GetPassKitUpdatableSerialsQuery,
                        PassKitSerialNumbersResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(
                        new GetPassKitUpdatableSerialsQuery(
                            deviceLibraryIdentifier,
                            passTypeIdentifier,
                            passesUpdatedSince
                        ),
                        ct
                    );
                    return res.IsFailure ? ToPassKitStatusResult(res, ctx)
                        : res.Value.SerialNumbers.Count == 0 ? Results.NoContent()
                        : Results.Ok(res.Value);
                }
            )
            .WithName("PassKitGetUpdatableSerials");
    }

    private static void MapUnregisterRoute(RouteGroupBuilder v1)
    {
        _ = v1.MapDelete(
                "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
                static async Task<HttpResult> (
                    string deviceLibraryIdentifier,
                    string passTypeIdentifier,
                    string serialNumber,
                    ICommandHandler<UnregisterPassKitDeviceCommand> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var auth = WalletPassSecurityTokens.ExtractApplePassHeaderToken(
                        ctx.Request.Headers.Authorization
                    );
                    var res = await handler.HandleAsync(
                        new UnregisterPassKitDeviceCommand(
                            deviceLibraryIdentifier,
                            passTypeIdentifier,
                            serialNumber,
                            auth
                        ),
                        ct
                    );
                    return res.IsFailure ? ToPassKitStatusResult(res, ctx) : Results.Ok();
                }
            )
            .WithName("PassKitUnregisterDevice");
    }

    private static void MapPassDeliveryAndLogRoutes(RouteGroupBuilder v1)
    {
        _ = v1.MapGet(
                "/passes/{passTypeIdentifier}/{serialNumber}",
                static async Task<HttpResult> (
                    string passTypeIdentifier,
                    string serialNumber,
                    IQueryHandler<GetPassKitLatestPassQuery, PassKitLatestPassResponse> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var auth = WalletPassSecurityTokens.ExtractApplePassHeaderToken(
                        ctx.Request.Headers.Authorization
                    );
                    var ifModified = ParseIfModifiedSince(ctx.Request.Headers.IfModifiedSince);
                    var origin = $"{ctx.Request.Scheme}://{ctx.Request.Host}";

                    var res = await handler.HandleAsync(
                        new GetPassKitLatestPassQuery(
                            passTypeIdentifier,
                            serialNumber,
                            auth,
                            ifModified,
                            origin
                        ),
                        ct
                    );
                    if (res.IsFailure)
                    {
                        return ToPassKitStatusResult(res, ctx);
                    }

                    ctx.Response.Headers.LastModified = res.Value.LastModifiedUtc.ToString(
                        "R",
                        CultureInfo.InvariantCulture
                    );
                    return res.Value.NotModified
                        ? Results.StatusCode(StatusCodes.Status304NotModified)
                        : Results.File(
                            res.Value.PkpassBytes,
                            "application/vnd.apple.pkpass",
                            $"{serialNumber}.pkpass"
                        );
                }
            )
            .WithName("PassKitGetLatestPass");

        _ = v1.MapPost("/log", static Results<Ok, BadRequest> (PassKitClientLogBody _) =>
                TypedResults.Ok()
            )
            .WithName("PassKitLogClientDiagnostics");
    }

    private static DateTimeOffset? ParseIfModifiedSince(string? rawHeader)
    {
        return
            !string.IsNullOrWhiteSpace(rawHeader)
            && DateTimeOffset.TryParse(
                rawHeader,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed
            )
            ? parsed
            : null;
    }

    private static HttpResult ToPassKitStatusResult(Result failure, HttpContext ctx)
    {
        return failure.PrimaryError.Type switch
        {
            ErrorType.Unauthorized => Results.StatusCode(StatusCodes.Status401Unauthorized),
            ErrorType.NotFound => Results.StatusCode(StatusCodes.Status404NotFound),
            ErrorType.Validation
            or ErrorType.Forbidden
            or ErrorType.Conflict
            or ErrorType.Failure
            or ErrorType.Unexpected
            or _ => CheckInProblemResultMapper.ToProblemResult(failure, ctx),
        };
    }
}
