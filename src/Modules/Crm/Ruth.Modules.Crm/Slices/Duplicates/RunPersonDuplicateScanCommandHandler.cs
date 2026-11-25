using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class RunPersonDuplicateScanCommandHandler(PersonDuplicateScannerService scanner)
    : ICommandHandler<RunPersonDuplicateScanCommand, DuplicateScanSummaryResponse>
{
    public async Task<Result<DuplicateScanSummaryResponse>> HandleAsync(
        RunPersonDuplicateScanCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var summary = await scanner.ScanAsync(command.IncludeInactive, cancellationToken);
        return Result.Success(summary);
    }
}
