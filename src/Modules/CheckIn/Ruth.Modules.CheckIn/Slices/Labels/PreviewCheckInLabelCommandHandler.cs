using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class PreviewCheckInLabelCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<PreviewCheckInLabelCommand, CheckInLabelPreviewResponse>
{
    public async Task<Result<CheckInLabelPreviewResponse>> HandleAsync(
        PreviewCheckInLabelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = command.Context ?? new CheckInLabelMergeContextDto();
        if (command.LabelId is { } idGuid && idGuid != Guid.Empty)
        {
            var labelId = CheckInLabelId.From(idGuid);
            var persisted = await dbContext
                .CheckInLabels.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == labelId, cancellationToken);
            return persisted is null
                ? Result.Failure<CheckInLabelPreviewResponse>(CheckInErrors.LabelNotFound)
                : Result.Success(
                    ZplLabelCompiler.Compile(
                        persisted.Name,
                        persisted.Layout,
                        context,
                        persisted.Copies,
                        persisted.CustomZplOverride
                    )
                );
        }

        var hasCustomZpl = !string.IsNullOrWhiteSpace(command.CustomZplOverride);
        var draftDto = command.DraftLayout
            ?? CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto();
        var schemaResult = CheckInLabelLayoutSchema.Create(
            draftDto,
            allowEmptyElements: hasCustomZpl
        );
        return schemaResult.IsFailure
            ? Result.Failure<CheckInLabelPreviewResponse>(schemaResult.Errors)
            : Result.Success(
                ZplLabelCompiler.Compile(
                    "Draft Label Preview",
                    schemaResult.Value,
                    context,
                    command.Copies,
                    command.CustomZplOverride
                )
            );
    }
}
