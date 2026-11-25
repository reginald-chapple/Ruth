using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetBinaryFileByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetBinaryFileByIdQuery, BinaryFileResponse>
{
    public async Task<Result<BinaryFileResponse>> HandleAsync(
        GetBinaryFileByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = BinaryFileId.From(query.BinaryFileId);
        var file = await dbContext
            .BinaryFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        return file is null
            ? CoreDocumentErrors.BinaryFileNotFound
            : Result.Success(FileMapper.ToResponse(file));
    }
}
