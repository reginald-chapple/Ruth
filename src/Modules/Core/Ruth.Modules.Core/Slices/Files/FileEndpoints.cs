using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Files;

internal static class FileEndpoints
{
    public static IEndpointRouteBuilder MapBinaryFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var files = endpoints
            .MapGroup("/api/core/files")
            .WithTags("Binary Files")
            .RequireAuthorization();
        MapFileRoutes(files);
        return endpoints;
    }

    private static void MapFileRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", GetFilesAsync)
            .WithName("CoreGetBinaryFiles")
            .WithSummary("List stored binary files")
            .Produces<IReadOnlyList<BinaryFileResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetFileByIdAsync)
            .WithName("CoreGetBinaryFileById")
            .WithSummary("Get binary file metadata")
            .Produces<BinaryFileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/upload", UploadFileAsync)
            .WithName("CoreUploadBinaryFile")
            .WithSummary("Upload a binary file stream to the configured storage provider")
            .DisableAntiforgery()
            .Produces<BinaryFileResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapGet("/{id:guid}/content", BinaryFileContentStreamer.DownloadAsync)
            .WithName("CoreDownloadBinaryFileContent")
            .WithSummary("Stream stored binary file content")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/{id:guid}/presigned-url", GetPresignedUrlAsync)
            .WithName("CoreGetBinaryFilePresignedUrl")
            .WithSummary("Generate a time-bounded S3/RustFS presigned download URL")
            .Produces<StoragePresignedUrl>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/crop", CropImageAsync)
            .WithName("CoreCropBinaryFileImage")
            .WithSummary("Crop and optionally resize an image binary file")
            .Produces<BinaryFileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapDelete("/{id:guid}", DeleteFileAsync)
            .WithName("CoreDeleteBinaryFile")
            .WithSummary("Delete a binary file from storage and metadata")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BinaryFileResponse>>, ProblemHttpResult>
    > GetFilesAsync(
        Guid? binaryFileTypeId,
        bool? isTemporary,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetBinaryFilesQuery,
            IReadOnlyList<BinaryFileResponse>
        >(new GetBinaryFilesQuery(binaryFileTypeId, isTemporary), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<BinaryFileResponse>, ProblemHttpResult>> GetFileByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetBinaryFileByIdQuery, BinaryFileResponse>(
            new GetBinaryFileByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<BinaryFileResponse>, ProblemHttpResult>
    > UploadFileAsync(
        Guid binaryFileTypeId,
        IFormFile file,
        bool? isTemporary,
        string? description,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        Guid? userId = Guid.TryParse(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var parsedUser
        )
            ? parsedUser
            : null;
        await using var stream = file.OpenReadStream();
        var result = await dispatcher.SendAsync<UploadBinaryFileCommand, BinaryFileResponse>(
            new UploadBinaryFileCommand(
                binaryFileTypeId,
                file.FileName,
                file.ContentType,
                stream,
                isTemporary ?? true,
                description,
                UploadedByUserId: userId
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/files/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<StoragePresignedUrl>, ProblemHttpResult>
    > GetPresignedUrlAsync(
        Guid id,
        int? expiresInMinutes,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetBinaryFilePresignedUrlQuery,
            StoragePresignedUrl
        >(new GetBinaryFilePresignedUrlQuery(id, expiresInMinutes ?? 15), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<BinaryFileResponse>, ProblemHttpResult>> CropImageAsync(
        Guid id,
        CropBinaryFileImageCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CropBinaryFileImageCommand, BinaryFileResponse>(
            command with
            {
                BinaryFileId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteFileAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteBinaryFileCommand, bool>(
            new DeleteBinaryFileCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
