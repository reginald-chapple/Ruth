using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.Transports;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Slices.Transports;

internal sealed class UpsertCommunicationTransportConfigCommandHandler(
    CommunicationDbContext dbContext
) : ICommandHandler<UpsertCommunicationTransportConfigCommand, CommunicationTransportConfigResponse>
{
    public async Task<Result<CommunicationTransportConfigResponse>> HandleAsync(
        UpsertCommunicationTransportConfigCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedKey = command.TransportKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var existing = await dbContext.CommunicationTransportConfigs.FirstOrDefaultAsync(
            c => c.TransportKey == normalizedKey,
            cancellationToken
        );

        var entityResult = existing is null
            ? CreateNew(command)
            : UpdateExisting(existing, command);
        if (entityResult.IsFailure)
        {
            return Result.Failure<CommunicationTransportConfigResponse>(entityResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ToMaskedResponse(entityResult.Value);
    }

    internal static CommunicationTransportConfigResponse ToMaskedResponse(
        CommunicationTransportConfig config
    )
    {
        return new CommunicationTransportConfigResponse(
            config.Id.Value,
            config.TransportKey,
            config.Channel,
            config.IsEnabled,
            config.DefaultFromAddress,
            config.DefaultFromName,
            SensitiveSettingRedactor.RedactDictionary(config.Credentials, redactAllValues: true),
            config.CreatedOnUtc,
            config.ModifiedOnUtc
        );
    }

    private Result<CommunicationTransportConfig> CreateNew(
        UpsertCommunicationTransportConfigCommand command
    )
    {
        var created = CommunicationTransportConfig.Create(
            command.TransportKey ?? string.Empty,
            command.Channel,
            command.IsEnabled,
            command.DefaultFromAddress,
            command.DefaultFromName,
            command.Credentials
        );
        if (created.IsSuccess)
        {
            _ = dbContext.CommunicationTransportConfigs.Add(created.Value);
        }

        return created;
    }

    private static Result<CommunicationTransportConfig> UpdateExisting(
        CommunicationTransportConfig existing,
        UpsertCommunicationTransportConfigCommand command
    )
    {
        var merged = SensitiveSettingRedactor.MergePreservingRedactedSecrets(
            command.Credentials,
            existing.Credentials
        );
        var updated = existing.Update(
            command.Channel,
            command.IsEnabled,
            command.DefaultFromAddress,
            command.DefaultFromName,
            merged
        );
        return updated.IsFailure
            ? Result.Failure<CommunicationTransportConfig>(updated.Errors)
            : existing;
    }
}
